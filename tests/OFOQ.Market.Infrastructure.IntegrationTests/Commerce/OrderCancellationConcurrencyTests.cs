using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Application.Commerce.Orders.Cancel;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;
using OFOQ.Market.Infrastructure.Persistence.Repositories;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class OrderCancellationConcurrencyTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task ConcurrentCancellation_RestocksInventoryOnlyOnce()
    {
        await _database.ResetAsync();

        var seed =
            await SeedOrderAsync();

        await using var contextA =
            CreateConcurrentContext(
                seed.TenantId);

        await using var contextB =
            CreateConcurrentContext(
                seed.TenantId);

        var handlerA =
            CreateHandler(
                contextA,
                seed.TenantId);

        var handlerB =
            CreateHandler(
                contextB,
                seed.TenantId);

        var command =
            new CancelOrderCommand(
                seed.OrderId,
                "Concurrent cancellation",
                seed.ActorUserId);

        var results =
            await Task.WhenAll(
                handlerA.HandleAsync(command),
                handlerB.HandleAsync(command));

        Assert.All(
            results,
            result =>
                Assert.NotNull(result));

        Assert.Single(
            results,
            result =>
                !result!.IsIdempotentReplay);

        Assert.Single(
            results,
            result =>
                result!.IsIdempotentReplay);

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var savedOrder =
            await verificationContext
                .Orders
                .Include("_inventoryMovements")
                .SingleAsync(
                    order =>
                        order.Id ==
                        seed.OrderId);

        var savedVariant =
            await verificationContext
                .ProductVariants
                .SingleAsync(
                    variant =>
                        variant.Id ==
                        seed.VariantId);

        Assert.Equal(
            OrderStatus.Cancelled,
            savedOrder.Status);

        Assert.Equal(
            OrderFulfillmentStatus.Cancelled,
            savedOrder.FulfillmentStatus);

        Assert.Equal(
            8,
            savedVariant.Inventory.Quantity);

        Assert.Equal(
            2,
            savedOrder.InventoryMovements.Count);

        var deduction =
            Assert.Single(
                savedOrder.InventoryMovements.Where(
                    movement =>
                        movement.Type ==
                        InventoryMovementType.CheckoutDeduction));

        Assert.Equal(
            -3,
            deduction.QuantityDelta);

        var restock =
            Assert.Single(
                savedOrder.InventoryMovements.Where(
                    movement =>
                        movement.Type ==
                        InventoryMovementType.OrderCancellationRestock));

        Assert.Equal(
            3,
            restock.QuantityDelta);

        Assert.Equal(
            5,
            restock.QuantityBefore);

        Assert.Equal(
            8,
            restock.QuantityAfter);
    }

    private CancelOrderHandler CreateHandler(
        MarketDbContext context,
        TenantId tenantId)
    {
        return new CancelOrderHandler(
            new OrderStateLockRepository(
                context),
            new TestCurrentTenant(
                tenantId),
            new EfTransactionExecutor(
                context),
            context,
            TimeProvider.System);
    }

    private MarketDbContext CreateConcurrentContext(
        TenantId tenantId)
    {
        using var templateContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantId));

        var connectionString =
            templateContext
                .Database
                .GetConnectionString();

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            throw new InvalidOperationException(
                "The integration test database connection string is unavailable.");
        }

        var builder =
            new NpgsqlConnectionStringBuilder(
                connectionString)
            {
                Pooling = false,
                Multiplexing = false
            };

        var options =
            new DbContextOptionsBuilder<MarketDbContext>()
                .UseNpgsql(
                    builder.ConnectionString)
                .Options;

        return new MarketDbContext(
            options,
            new TestCurrentTenant(
                tenantId));
    }

    private async Task<SeedData> SeedOrderAsync()
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Cancellation Concurrency Store",
                $"cancel-concurrency-{Guid.NewGuid():N}",
                now);

        await using (var context =
                     _database.CreateContext())
        {
            context.Tenants.Add(
                tenant);

            await context.SaveChangesAsync();
        }

        var actorUserId =
            UserId.New();

        var customerUserId =
            UserId.New();

        var product =
            Product.Create(
                tenant.Id,
                "Cancellation Product",
                $"cancel-product-{Guid.NewGuid():N}",
                Money.Create(
                    100m,
                    "SAR"),
                now);

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            context.Products.Add(
                product);

            await context.SaveChangesAsync();
        }

        var variant =
            ProductVariant.Create(
                tenant.Id,
                product.Id,
                "Default",
                ProductSku.Create(
                    $"CANCEL-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "SAR"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 8),
                now,
                isDefault: true);

        var quantityBefore =
            variant.Inventory.Quantity;

        variant.DecreaseStock(
            3,
            now.AddMinutes(1),
            customerUserId.Value);

        var quantityAfter =
            variant.Inventory.Quantity;

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            context.ProductVariants.Add(
                variant);

            await context.SaveChangesAsync();
        }

        var cart =
            Cart.Create(
                tenant.Id,
                customerUserId,
                now,
                customerUserId.Value);

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            context.Carts.Add(
                cart);

            await context.SaveChangesAsync();
        }

        var order =
            Order.Create(
                tenant.Id,
                customerUserId,
                cart.Id,
                CurrencyCode.Create(
                    "SAR"),
                new[]
                {
                    new OrderItemSnapshot(
                        product.Id,
                        variant.Id,
                        product.Name,
                        variant.Name,
                        variant.Sku.Value,
                        Money.Create(
                            100m,
                            "SAR"),
                        3)
                },
                now,
                customerUserId.Value);

        order.RecordCheckoutInventoryDeduction(
            product.Id,
            variant.Id,
            quantityBefore,
            quantityAfter,
            now.AddMinutes(1),
            customerUserId.Value);

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            context.Orders.Add(
                order);

            await context.SaveChangesAsync();
        }

        return new SeedData(
            tenant.Id,
            actorUserId,
            order.Id,
            variant.Id);
    }

    private sealed record SeedData(
        TenantId TenantId,
        UserId ActorUserId,
        OrderId OrderId,
        ProductVariantId VariantId);
}