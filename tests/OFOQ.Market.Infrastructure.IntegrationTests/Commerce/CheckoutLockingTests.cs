using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;
using OFOQ.Market.Infrastructure.Persistence.Repositories;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class CheckoutLockingTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task VariantForUpdate_SecondTransactionWaits_ThenReadsCommittedStock()
    {
        await _database.ResetAsync();

        var seed =
            await SeedCheckoutDataAsync();

        await using var contextA =
            CreateConcurrentContext(
                seed.TenantId);

        await using var contextB =
            CreateConcurrentContext(
                seed.TenantId);

        /*
         * Open both physical PostgreSQL connections
         * before any row lock is acquired.
         *
         * This guarantees the test is measuring the
         * row lock rather than connection-pool waiting.
         */
        await contextA.Database
            .OpenConnectionAsync();

        await contextB.Database
            .OpenConnectionAsync();

        await using var transactionA =
            await contextA.Database
                .BeginTransactionAsync();

        await using var transactionB =
            await contextB.Database
                .BeginTransactionAsync();

        var repositoryA =
            new CheckoutLockRepository(
                contextA);

        var lockedVariantsA =
            await repositoryA
                .GetVariantsForUpdateAsync(
                    new[]
                    {
                        seed.VariantId
                    });

        var variantA =
            Assert.Single(
                lockedVariantsA);

        Assert.Equal(
            10,
            variantA.Inventory.Quantity);

        /*
         * Transaction A changes stock from 10 -> 6.
         *
         * SaveChanges sends the UPDATE to PostgreSQL,
         * but Transaction A remains open and continues
         * to own the row lock.
         */
        variantA.DecreaseStock(
            4,
            DateTimeOffset.UtcNow,
            seed.CustomerUserId.Value);

        await contextA.SaveChangesAsync();

        var repositoryB =
            new CheckoutLockRepository(
                contextB);

        using var secondOperationCancellation =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(15));

        /*
         * Do not await yet.
         *
         * Transaction B should block inside PostgreSQL
         * on SELECT ... FOR UPDATE.
         */
        var blockedReadTask =
            repositoryB
                .GetVariantsForUpdateAsync(
                    new[]
                    {
                        seed.VariantId
                    },
                    secondOperationCancellation.Token);

        await Task.Delay(
            TimeSpan.FromMilliseconds(300));

        Assert.False(
            blockedReadTask.IsCompleted);

        /*
         * Releasing Transaction A's lock must allow
         * Transaction B to continue.
         */
        await transactionA
            .CommitAsync();

        var lockedVariantsB =
            await blockedReadTask
                .WaitAsync(
                    TimeSpan.FromSeconds(10));

        var variantB =
            Assert.Single(
                lockedVariantsB);

        /*
         * B must observe the value committed by A,
         * not the stale stock value from before
         * waiting on the lock.
         */
        Assert.Equal(
            6,
            variantB.Inventory.Quantity);

        await transactionB
            .CommitAsync();

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var persistedVariant =
            await verificationContext
                .ProductVariants
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.VariantId);

        Assert.Equal(
            6,
            persistedVariant.Inventory.Quantity);
    }

    [Fact]
    public async Task ActiveCartForUpdate_AfterFirstCheckoutConvertsCart_SecondTransactionGetsNoActiveCart()
    {
        await _database.ResetAsync();

        var seed =
            await SeedCheckoutDataAsync();

        await using var contextA =
            CreateConcurrentContext(
                seed.TenantId);

        await using var contextB =
            CreateConcurrentContext(
                seed.TenantId);

        /*
         * Establish both physical connections before
         * acquiring the Cart row lock.
         */
        await contextA.Database
            .OpenConnectionAsync();

        await contextB.Database
            .OpenConnectionAsync();

        await using var transactionA =
            await contextA.Database
                .BeginTransactionAsync();

        await using var transactionB =
            await contextB.Database
                .BeginTransactionAsync();

        var repositoryA =
            new CheckoutLockRepository(
                contextA);

        var cartA =
            await repositoryA
                .GetActiveCartForUpdateAsync(
                    seed.CustomerUserId);

        Assert.NotNull(
            cartA);

        Assert.Equal(
            CartStatus.Active,
            cartA.Status);

        Assert.Single(
            cartA.Items);

        /*
         * Simulate the final Cart mutation performed
         * by checkout.
         */
        cartA.MarkConverted(
            DateTimeOffset.UtcNow,
            seed.CustomerUserId.Value);

        await contextA.SaveChangesAsync();

        var repositoryB =
            new CheckoutLockRepository(
                contextB);

        using var secondOperationCancellation =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(15));

        /*
         * Transaction B asks for the same ACTIVE Cart.
         *
         * PostgreSQL must wait because Transaction A
         * currently owns its row lock.
         */
        var blockedCartReadTask =
            repositoryB
                .GetActiveCartForUpdateAsync(
                    seed.CustomerUserId,
                    secondOperationCancellation.Token);

        await Task.Delay(
            TimeSpan.FromMilliseconds(300));

        Assert.False(
            blockedCartReadTask.IsCompleted);

        /*
         * After A commits, B re-evaluates the locked
         * row. The Cart is now Converted and therefore
         * must not satisfy status = Active.
         */
        await transactionA
            .CommitAsync();

        var cartB =
            await blockedCartReadTask
                .WaitAsync(
                    TimeSpan.FromSeconds(10));

        Assert.Null(
            cartB);

        await transactionB
            .CommitAsync();

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var persistedCart =
            await verificationContext
                .Carts
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.CartId);

        Assert.Equal(
            CartStatus.Converted,
            persistedCart.Status);
    }

    private MarketDbContext CreateConcurrentContext(
        TenantId tenantId)
    {
        /*
         * Start from the same validated test connection
         * string already owned by IntegrationTestDatabase.
         *
         * We do not print or expose that connection string.
         */
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
                /*
                 * Concurrency tests require two genuinely
                 * independent physical PostgreSQL sessions.
                 *
                 * This also prevents a small configured
                 * connection pool from becoming the thing
                 * being tested instead of FOR UPDATE.
                 */
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

    private async Task<CheckoutSeed>
        SeedCheckoutDataAsync()
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Checkout Lock Store",
                $"checkout-lock-{Guid.NewGuid():N}",
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
                "Checkout Product",
                $"checkout-product-{Guid.NewGuid():N}",
                Money.Create(
                    30m,
                    "USD"),
                now);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Products.Add(
                product);

            await tenantContext.SaveChangesAsync();
        }

        var variant =
            ProductVariant.Create(
                tenant.Id,
                product.Id,
                "Default",
                ProductSku.Create(
                    $"CHECKOUT-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 10,
                    lowStockThreshold: 2),
                now,
                isDefault:
                    true);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.ProductVariants.Add(
                variant);

            await tenantContext.SaveChangesAsync();
        }

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
                30m,
                "USD"),
            2,
            now,
            customerUserId.Value);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Carts.Add(
                cart);

            await tenantContext.SaveChangesAsync();
        }

        return new CheckoutSeed(
            tenant.Id,
            customerUserId,
            cart.Id,
            variant.Id);
    }

    private sealed record CheckoutSeed(
        TenantId TenantId,
        UserId CustomerUserId,
        CartId CartId,
        ProductVariantId VariantId);
}