using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Application.Commerce.Checkout;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;
using OFOQ.Market.Infrastructure.Persistence.Repositories;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class AtomicCheckoutIntegrationTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Checkout_Success_PersistsOrderInventoryAndConvertedCartAtomically()
    {
        await _database.ResetAsync();

        var seed =
            await SeedSingleCartAsync(
                stockQuantity: 6,
                cartQuantity: 2,
                productPrice: 30m,
                cartSnapshotPrice: 10m);

        await using var context =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var handler =
            CreateHandler(
                context,
                seed.TenantId);

        var result =
            await handler.HandleAsync(
                new CheckoutCommand(
                    seed.CustomerUserId,
                    "integration-success"));

        Assert.Equal(
            "Pending",
            result.Status);

        Assert.Equal(
            60m,
            result.TotalAmount);

        Assert.Equal(
            30m,
            Assert.Single(
                    result.Items)
                .UnitPrice);

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var order =
            await verificationContext.Orders
                .Include("_items")
                .SingleAsync();

        var cart =
            await verificationContext.Carts
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.CartId);

        var variant =
            await verificationContext.ProductVariants
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.VariantId);

        Assert.Equal(
            result.OrderId,
            order.Id);

        Assert.Equal(
            CartStatus.Converted,
            cart.Status);

        Assert.Equal(
            4,
            variant.Inventory.Quantity);
    }

    [Fact]
    public async Task Checkout_ConcurrentSameKey_CreatesOneOrderAndDecreasesStockOnce()
    {
        await _database.ResetAsync();

        var seed =
            await SeedSingleCartAsync(
                stockQuantity: 5,
                cartQuantity: 2,
                productPrice: 20m,
                cartSnapshotPrice: 20m);

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

        var firstTask =
            handlerA.HandleAsync(
                new CheckoutCommand(
                    seed.CustomerUserId,
                    "same-concurrent-key"));

        var secondTask =
            handlerB.HandleAsync(
                new CheckoutCommand(
                    seed.CustomerUserId,
                    "same-concurrent-key"));

        var results =
            await Task.WhenAll(
                firstTask,
                secondTask);

        Assert.Equal(
            results[0].OrderId,
            results[1].OrderId);

        Assert.Single(
            results,
            item =>
                !item.IsIdempotentReplay);

        Assert.Single(
            results,
            item =>
                item.IsIdempotentReplay);

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        Assert.Equal(
            1,
            await verificationContext.Orders.CountAsync());

        var variant =
            await verificationContext.ProductVariants
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.VariantId);

        Assert.Equal(
            3,
            variant.Inventory.Quantity);
    }

    [Fact]
    public async Task Checkout_TwoCustomersCompeteForLastUnit_OnlyOneSucceeds()
    {
        await _database.ResetAsync();

        var seed =
            await SeedTwoCustomersForLastUnitAsync();

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

        var outcomes =
            await Task.WhenAll(
                RunCheckoutAsync(
                    handlerA,
                    seed.CustomerA,
                    "last-unit-a"),
                RunCheckoutAsync(
                    handlerB,
                    seed.CustomerB,
                    "last-unit-b"));

        Assert.Single(
            outcomes,
            outcome =>
                outcome.Succeeded);

        Assert.Single(
            outcomes,
            outcome =>
                outcome.Exception is
                    CheckoutInsufficientStockException);

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        Assert.Equal(
            1,
            await verificationContext.Orders.CountAsync());

        var variant =
            await verificationContext.ProductVariants
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.VariantId);

        Assert.Equal(
            0,
            variant.Inventory.Quantity);

        var carts =
            await verificationContext.Carts
                .OrderBy(
                    item =>
                        item.CreatedAtUtc)
                .ToListAsync();

        Assert.Equal(
            2,
            carts.Count);

        Assert.Single(
            carts,
            cart =>
                cart.Status ==
                CartStatus.Converted);

        Assert.Single(
            carts,
            cart =>
                cart.Status ==
                CartStatus.Active);
    }

    [Fact]
    public async Task Checkout_WhenPersistenceFails_RollsBackInventoryCartAndNewOrder()
    {
        await _database.ResetAsync();

        var seed =
            await SeedSingleCartAsync(
                stockQuantity: 5,
                cartQuantity: 2,
                productPrice: 10m,
                cartSnapshotPrice: 10m);

        /*
         * Seed an existing Order for the same source Cart while
         * intentionally leaving the Cart active. The database
         * permits this historical/inconsistent setup, but the
         * unique source-cart index guarantees the checkout Save
         * will fail late, after in-memory stock/cart mutations.
         * This lets us prove the transaction rolls all DB writes
         * back together.
         */
        await using (var seedOrderContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            var repository =
                new OrderRepository(
                    seedOrderContext);

            await repository.AddAsync(
                CreateOrder(
                    seed,
                    seed.CartId),
                "preexisting-order-key");

            await seedOrderContext.SaveChangesAsync();
        }

        await using (var checkoutContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            var handler =
                CreateHandler(
                    checkoutContext,
                    seed.TenantId);

            await Assert.ThrowsAsync<
                DbUpdateException>(
                    () =>
                        handler.HandleAsync(
                            new CheckoutCommand(
                                seed.CustomerUserId,
                                "late-persistence-failure")));
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var cart =
            await verificationContext.Carts
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.CartId);

        var variant =
            await verificationContext.ProductVariants
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.VariantId);

        Assert.Equal(
            CartStatus.Active,
            cart.Status);

        Assert.Equal(
            5,
            variant.Inventory.Quantity);

        Assert.Equal(
            1,
            await verificationContext.Orders.CountAsync());
    }

    [Fact]
    public async Task Database_RejectsSameCheckoutIdempotencyKeyForDifferentCartsOfSameCustomer()
    {
        await _database.ResetAsync();

        var seed =
            await SeedSingleCartAsync(
                stockQuantity: 10,
                cartQuantity: 1,
                productPrice: 10m,
                cartSnapshotPrice: 10m);

        var secondCart =
            Cart.Create(
                seed.TenantId,
                seed.CustomerUserId,
                DateTimeOffset.UtcNow.AddMinutes(1),
                seed.CustomerUserId.Value);

        secondCart.AddItem(
            seed.ProductId,
            seed.VariantId,
            Money.Create(
                10m,
                "USD"),
            1,
            DateTimeOffset.UtcNow.AddMinutes(1),
            seed.CustomerUserId.Value);

        /*
         * The database allows only one active Cart per
         * customer. Convert the original in its own save,
         * then persist the second Cart. This test is about
         * the Order idempotency constraint, not Cart indexes.
         */
        await using (var conversionContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            var original =
                await conversionContext.Carts
                    .Include("_items")
                    .SingleAsync(
                        item =>
                            item.Id ==
                            seed.CartId);

            original.MarkConverted(
                DateTimeOffset.UtcNow,
                seed.CustomerUserId.Value);

            await conversionContext.SaveChangesAsync();
        }

        await using (var cartContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            cartContext.Carts.Add(
                secondCart);

            await cartContext.SaveChangesAsync();
        }

        var firstOrder =
            CreateOrder(
                seed,
                seed.CartId);

        await using (var firstContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            var repository =
                new OrderRepository(
                    firstContext);

            await repository.AddAsync(
                firstOrder,
                "db-unique-key");

            await firstContext.SaveChangesAsync();
        }

        var secondOrder =
            CreateOrder(
                seed,
                secondCart.Id);

        await using var secondContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var secondRepository =
            new OrderRepository(
                secondContext);

        await secondRepository.AddAsync(
            secondOrder,
            "db-unique-key");

        var exception =
            await Assert.ThrowsAsync<
                DbUpdateException>(
                    () =>
                        secondContext.SaveChangesAsync());

        var postgresException =
            Assert.IsType<
                PostgresException>(
                    exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.UniqueViolation,
            postgresException.SqlState);

        Assert.Equal(
            "ux_commerce_orders_checkout_idempotency",
            postgresException.ConstraintName);
    }

    private CheckoutHandler CreateHandler(
        MarketDbContext context,
        TenantId tenantId)
    {
        return new CheckoutHandler(
            new CartRepository(
                context),
            new CheckoutLockRepository(
                context),
            new OrderRepository(
                context),
            new ProductRepository(
                context),
            new ProductOptionRepository(
                context),
            new ProductVariantOptionValueRepository(
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
            templateContext.Database
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
            new DbContextOptionsBuilder<
                    MarketDbContext>()
                .UseNpgsql(
                    builder.ConnectionString)
                .Options;

        return new MarketDbContext(
            options,
            new TestCurrentTenant(
                tenantId));
    }

    private async Task<SingleCartSeed> SeedSingleCartAsync(
        int stockQuantity,
        int cartQuantity,
        decimal productPrice,
        decimal cartSnapshotPrice)
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Atomic Checkout Store",
                $"atomic-{Guid.NewGuid():N}",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.Add(
                tenant);

            await setupContext.SaveChangesAsync();
        }

        var product =
            Product.Create(
                tenant.Id,
                "Atomic Product",
                $"atomic-product-{Guid.NewGuid():N}",
                Money.Create(
                    productPrice,
                    "USD"),
                now);

        product.Publish(
            now.AddSeconds(1));

        var variant =
            ProductVariant.Create(
                tenant.Id,
                product.Id,
                "Default",
                ProductSku.Create(
                    $"ATOMIC-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: stockQuantity),
                now,
                isDefault:
                    true);

        var customerUserId =
            UserId.New();

        var cart =
            Cart.Create(
                tenant.Id,
                customerUserId,
                now,
                customerUserId.Value);

        cart.AddItem(
            product.Id,
            variant.Id,
            Money.Create(
                cartSnapshotPrice,
                "USD"),
            cartQuantity,
            now,
            customerUserId.Value);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Products.Add(
                product);

            tenantContext.ProductVariants.Add(
                variant);

            tenantContext.Carts.Add(
                cart);

            await tenantContext.SaveChangesAsync();
        }

        return new SingleCartSeed(
            tenant.Id,
            customerUserId,
            product.Id,
            variant.Id,
            cart.Id,
            product.Name,
            variant.Name,
            variant.Sku.Value);
    }

    private async Task<TwoCustomerSeed>
        SeedTwoCustomersForLastUnitAsync()
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Last Unit Store",
                $"last-unit-{Guid.NewGuid():N}",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.Add(
                tenant);

            await setupContext.SaveChangesAsync();
        }

        var product =
            Product.Create(
                tenant.Id,
                "Last Unit Product",
                $"last-unit-product-{Guid.NewGuid():N}",
                Money.Create(
                    50m,
                    "USD"),
                now);

        product.Publish(
            now.AddSeconds(1));

        var variant =
            ProductVariant.Create(
                tenant.Id,
                product.Id,
                "Default",
                ProductSku.Create(
                    $"LAST-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 1),
                now,
                isDefault:
                    true);

        var customerA =
            UserId.New();

        var customerB =
            UserId.New();

        var cartA =
            Cart.Create(
                tenant.Id,
                customerA,
                now,
                customerA.Value);

        cartA.AddItem(
            product.Id,
            variant.Id,
            product.Price,
            1,
            now,
            customerA.Value);

        var cartB =
            Cart.Create(
                tenant.Id,
                customerB,
                now.AddMilliseconds(1),
                customerB.Value);

        cartB.AddItem(
            product.Id,
            variant.Id,
            product.Price,
            1,
            now.AddMilliseconds(1),
            customerB.Value);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Products.Add(
                product);

            tenantContext.ProductVariants.Add(
                variant);

            tenantContext.Carts.AddRange(
                cartA,
                cartB);

            await tenantContext.SaveChangesAsync();
        }

        return new TwoCustomerSeed(
            tenant.Id,
            customerA,
            customerB,
            variant.Id);
    }

    private static async Task<CheckoutOutcome> RunCheckoutAsync(
        CheckoutHandler handler,
        UserId customerUserId,
        string idempotencyKey)
    {
        try
        {
            var result =
                await handler.HandleAsync(
                    new CheckoutCommand(
                        customerUserId,
                        idempotencyKey));

            return new CheckoutOutcome(
                true,
                result.OrderId.Value,
                null);
        }
        catch (Exception exception)
        {
            return new CheckoutOutcome(
                false,
                null,
                exception);
        }
    }

    private static OFOQ.Market.Domain.Commerce.Orders.Order CreateOrder(
        SingleCartSeed seed,
        CartId cartId)
    {
        return OFOQ.Market.Domain.Commerce.Orders.Order.Create(
            seed.TenantId,
            seed.CustomerUserId,
            cartId,
            CurrencyCode.Create(
                "USD"),
            new[]
            {
                new OFOQ.Market.Domain.Commerce.Orders.OrderItemSnapshot(
                    seed.ProductId,
                    seed.VariantId,
                    seed.ProductName,
                    seed.VariantName,
                    seed.Sku,
                    Money.Create(
                        10m,
                        "USD"),
                    1)
            },
            DateTimeOffset.UtcNow,
            seed.CustomerUserId.Value);
    }

    private sealed record SingleCartSeed(
        TenantId TenantId,
        UserId CustomerUserId,
        ProductId ProductId,
        ProductVariantId VariantId,
        CartId CartId,
        string ProductName,
        string VariantName,
        string Sku);

    private sealed record TwoCustomerSeed(
        TenantId TenantId,
        UserId CustomerA,
        UserId CustomerB,
        ProductVariantId VariantId);

    private sealed record CheckoutOutcome(
        bool Succeeded,
        Guid? OrderId,
        Exception? Exception);
}
