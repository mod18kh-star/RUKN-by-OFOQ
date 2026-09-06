using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Application.Commerce.Payments.CreateIntent;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;
using OFOQ.Market.Infrastructure.Persistence.Repositories;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class PaymentApiConcurrencyTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task CreateIntent_ConcurrentSameKey_CreatesOnePaymentAndOneIntent()
    {
        await _database.ResetAsync();
        var seed = await SeedOrderAsync();

        await using var contextA = CreateConcurrentContext(seed.TenantId);
        await using var contextB = CreateConcurrentContext(seed.TenantId);

        var handlerA = CreateHandler(contextA, seed.TenantId);
        var handlerB = CreateHandler(contextB, seed.TenantId);

        var command = new CreatePaymentIntentCommand(
            seed.Order.Id,
            seed.CustomerUserId,
            seed.PaymentMethodId,
            "concurrent-payment-key");

        var results = await Task.WhenAll(
            handlerA.HandleAsync(command),
            handlerB.HandleAsync(command));

        Assert.Equal(results[0].PaymentIntentId, results[1].PaymentIntentId);
        Assert.Equal(results[0].PaymentId, results[1].PaymentId);
        Assert.Single(results, item => item.IsIdempotentReplay);
        Assert.Single(results, item => !item.IsIdempotentReplay);

        await using var verificationContext = _database.CreateContext(
            new TestCurrentTenant(seed.TenantId));

        Assert.Equal(1, await verificationContext.Payments.CountAsync());
        Assert.Equal(1, await verificationContext.PaymentIntents.CountAsync());
    }

    private CreatePaymentIntentHandler CreateHandler(
        MarketDbContext context,
        TenantId tenantId)
    {
        return new CreatePaymentIntentHandler(
            new PaymentRepository(context),
            new PaymentIntentRepository(context),
            new TenantPaymentMethodRepository(context),
            new TenantPaymentCapabilityRepository(context),
            new PaymentCreationLockRepository(context),
            new TestCurrentTenant(tenantId),
            new EfTransactionExecutor(context),
            context,
            TimeProvider.System);
    }

    private MarketDbContext CreateConcurrentContext(TenantId tenantId)
    {
        using var templateContext = _database.CreateContext(
            new TestCurrentTenant(tenantId));

        var connectionString = templateContext.Database.GetConnectionString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The integration test database connection string is unavailable.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = false,
            Multiplexing = false
        };

        var options = new DbContextOptionsBuilder<MarketDbContext>()
            .UseNpgsql(builder.ConnectionString)
            .Options;

        return new MarketDbContext(
            options,
            new TestCurrentTenant(tenantId));
    }

    private async Task<SeedData> SeedOrderAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var tenant = Tenant.Create(
            "Payment API Concurrency Store",
            $"payment-concurrency-{Guid.NewGuid():N}",
            now);

        await using (var setupContext = _database.CreateContext())
        {
            setupContext.Tenants.Add(tenant);
            await setupContext.SaveChangesAsync();
        }

        var product = Product.Create(
            tenant.Id,
            "Payment Product",
            $"payment-product-{Guid.NewGuid():N}",
            Money.Create(25m, "USD"),
            now);

        await using (var context = _database.CreateContext(
                     new TestCurrentTenant(tenant.Id)))
        {
            context.Products.Add(product);
            await context.SaveChangesAsync();
        }

        var variant = ProductVariant.Create(
            tenant.Id,
            product.Id,
            "Default",
            ProductSku.Create($"PAYAPI-{Guid.NewGuid():N}"),
            CurrencyCode.Create("USD"),
            Inventory.Create(trackInventory: true, quantity: 10),
            now,
            priceOverride: Money.Create(25m, "USD"),
            isDefault: true);

        await using (var context = _database.CreateContext(
                     new TestCurrentTenant(tenant.Id)))
        {
            context.ProductVariants.Add(variant);
            await context.SaveChangesAsync();
        }

        var customerUserId = UserId.New();
        var cart = Cart.Create(
            tenant.Id,
            customerUserId,
            now,
            customerUserId.Value);

        cart.AddItem(
            product.Id,
            variant.Id,
            Money.Create(25m, "USD"),
            2,
            now,
            customerUserId.Value);

        await using (var context = _database.CreateContext(
                     new TestCurrentTenant(tenant.Id)))
        {
            context.Carts.Add(cart);
            await context.SaveChangesAsync();
        }

        var order = Order.Create(
            tenant.Id,
            customerUserId,
            cart.Id,
            CurrencyCode.Create("USD"),
            new[]
            {
                new OrderItemSnapshot(
                    product.Id,
                    variant.Id,
                    product.Name,
                    variant.Name,
                    variant.Sku.Value,
                    Money.Create(25m, "USD"),
                    2)
            },
            now,
            customerUserId.Value);

        var method = TenantPaymentMethod.Create(
            tenant.Id,
            PaymentMethodType.Electronic,
            "provider-a",
            "Provider A",
            "SY",
            "USD",
            null,
            null,
            now,
            customerUserId.Value);

        await using (var context = _database.CreateContext(
                     new TestCurrentTenant(tenant.Id)))
        {
            context.Orders.Add(order);
            context.TenantPaymentMethods.Add(method);
            await context.SaveChangesAsync();
        }

        return new SeedData(
            tenant.Id,
            customerUserId,
            order,
            method.Id);
    }

    private sealed record SeedData(
        TenantId TenantId,
        UserId CustomerUserId,
        Order Order,
        TenantPaymentMethodId PaymentMethodId);
}
