using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class OrderPersistenceTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Order_RoundTrip_PreservesSnapshotTotalsAndSourceCart()
    {
        await _database.ResetAsync();

        var seed =
            await SeedCheckoutDataAsync(
                "Store A");

        var order =
            CreateOrder(
                seed);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            tenantContext.Orders.Add(
                order);

            await tenantContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var saved =
            await verificationContext
                .Orders
                .Include("_items")
                .SingleAsync(
                    item =>
                        item.Id ==
                        order.Id);

        Assert.Equal(
            seed.TenantId,
            saved.TenantId);

        Assert.Equal(
            seed.CustomerUserId,
            saved.CustomerUserId);

        Assert.Equal(
            seed.CartId,
            saved.SourceCartId);

        Assert.Equal(
            OrderStatus.Pending,
            saved.Status);

        Assert.Equal(
            "USD",
            saved.Currency.Value);

        Assert.Equal(
            2,
            saved.TotalQuantity);

        Assert.Equal(
            60m,
            saved.TotalAmount);

        var item =
            Assert.Single(
                saved.Items);

        Assert.Equal(
            seed.ProductId,
            item.ProductId);

        Assert.Equal(
            seed.VariantId,
            item.ProductVariantId);

        Assert.Equal(
            seed.ProductName,
            item.ProductName);

        Assert.Equal(
            seed.VariantName,
            item.VariantName);

        Assert.Equal(
            seed.Sku,
            item.Sku);

        Assert.Equal(
            30m,
            item.UnitPrice.Amount);

        Assert.Equal(
            "USD",
            item.UnitPrice.Currency.Value);

        Assert.Equal(
            2,
            item.Quantity);

        Assert.Equal(
            60m,
            item.LineTotal);
    }

    [Fact]
    public async Task OrderQuery_ReturnsOnlyCurrentTenantOrders()
    {
        await _database.ResetAsync();

        var seedA =
            await SeedCheckoutDataAsync(
                "Store A");

        var seedB =
            await SeedCheckoutDataAsync(
                "Store B");

        var orderA =
            CreateOrder(
                seedA);

        var orderB =
            CreateOrder(
                seedB);

        await using (var contextA =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seedA.TenantId)))
        {
            contextA.Orders.Add(
                orderA);

            await contextA.SaveChangesAsync();
        }

        await using (var contextB =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seedB.TenantId)))
        {
            contextB.Orders.Add(
                orderB);

            await contextB.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seedA.TenantId));

        var orders =
            await verificationContext
                .Orders
                .Include("_items")
                .ToListAsync();

        var saved =
            Assert.Single(
                orders);

        Assert.Equal(
            orderA.Id,
            saved.Id);

        Assert.Equal(
            seedA.TenantId,
            saved.TenantId);

        Assert.Equal(
            seedA.CustomerUserId,
            saved.CustomerUserId);

        var item =
            Assert.Single(
                saved.Items);

        Assert.Equal(
            seedA.ProductId,
            item.ProductId);
    }

    [Fact]
    public async Task Query_WithoutTenantContext_ReturnsNoOrdersOrOrderItems()
    {
        await _database.ResetAsync();

        var seed =
            await SeedCheckoutDataAsync(
                "Store A");

        var order =
            CreateOrder(
                seed);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            tenantContext.Orders.Add(
                order);

            await tenantContext.SaveChangesAsync();
        }

        await using var noTenantContext =
            _database.CreateContext();

        var orders =
            await noTenantContext
                .Orders
                .ToListAsync();

        var orderItems =
            await noTenantContext
                .OrderItems
                .ToListAsync();

        Assert.Empty(
            orders);

        Assert.Empty(
            orderItems);
    }

    [Fact]
    public async Task Database_RejectsSecondOrderFromSameCart()
    {
        await _database.ResetAsync();

        var seed =
            await SeedCheckoutDataAsync(
                "Store A");

        var firstOrder =
            CreateOrder(
                seed);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            tenantContext.Orders.Add(
                firstOrder);

            await tenantContext.SaveChangesAsync();
        }

        var secondOrder =
            CreateOrder(
                seed);

        await using var duplicateContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        duplicateContext.Orders.Add(
            secondOrder);

        var exception =
            await Assert.ThrowsAsync<
                DbUpdateException>(
                    () =>
                        duplicateContext
                            .SaveChangesAsync());

        var postgresException =
            Assert.IsType<
                PostgresException>(
                    exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.UniqueViolation,
            postgresException.SqlState);

        Assert.Equal(
            "ux_commerce_orders_tenant_source_cart",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_RejectsOrderReferencingCartFromAnotherTenant()
    {
        await _database.ResetAsync();

        var seedA =
            await SeedCheckoutDataAsync(
                "Store A");

        var seedB =
            await SeedCheckoutDataAsync(
                "Store B");

        /*
         * The Order belongs to Tenant A and its catalog
         * references also belong to Tenant A.
         *
         * Only the source Cart intentionally belongs
         * to Tenant B.
         */
        var illegalOrder =
            Order.Create(
                seedA.TenantId,
                seedA.CustomerUserId,
                seedB.CartId,
                CurrencyCode.Create(
                    "USD"),
                new[]
                {
                    CreateSnapshot(
                        seedA)
                },
                DateTimeOffset.UtcNow,
                seedA.CustomerUserId.Value);

        await using var contextA =
            _database.CreateContext(
                new TestCurrentTenant(
                    seedA.TenantId));

        contextA.Orders.Add(
            illegalOrder);

        var exception =
            await Assert.ThrowsAsync<
                DbUpdateException>(
                    () =>
                        contextA
                            .SaveChangesAsync());

        var postgresException =
            Assert.IsType<
                PostgresException>(
                    exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.ForeignKeyViolation,
            postgresException.SqlState);

        Assert.Equal(
            "fk_commerce_orders_source_cart",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_RejectsOrderItemReferencingCatalogFromAnotherTenant()
    {
        await _database.ResetAsync();

        var seedA =
            await SeedCheckoutDataAsync(
                "Store A");

        var seedB =
            await SeedCheckoutDataAsync(
                "Store B");

        /*
         * Source Cart belongs to Tenant A, therefore
         * the Order itself is valid.
         *
         * Its snapshot IDs intentionally point to
         * Tenant B's Product and Variant.
         */
        var illegalOrder =
            Order.Create(
                seedA.TenantId,
                seedA.CustomerUserId,
                seedA.CartId,
                CurrencyCode.Create(
                    "USD"),
                new[]
                {
                    new OrderItemSnapshot(
                        seedB.ProductId,
                        seedB.VariantId,
                        seedB.ProductName,
                        seedB.VariantName,
                        seedB.Sku,
                        Money.Create(
                            30m,
                            "USD"),
                        2)
                },
                DateTimeOffset.UtcNow,
                seedA.CustomerUserId.Value);

        await using var contextA =
            _database.CreateContext(
                new TestCurrentTenant(
                    seedA.TenantId));

        contextA.Orders.Add(
            illegalOrder);

        var exception =
            await Assert.ThrowsAsync<
                DbUpdateException>(
                    () =>
                        contextA
                            .SaveChangesAsync());

        var postgresException =
            Assert.IsType<
                PostgresException>(
                    exception.InnerException);

        Assert.Equal(
            PostgresErrorCodes.ForeignKeyViolation,
            postgresException.SqlState);

        /*
         * Both catalog constraints protect this row.
         * PostgreSQL may report either one first.
         */
        Assert.True(
            postgresException.ConstraintName is
                "fk_commerce_order_items_products"
                or
                "fk_commerce_order_items_variants");
    }

    [Fact]
    public async Task OrderSnapshot_RemainsUnchanged_WhenCatalogRowsChange()
    {
        await _database.ResetAsync();

        var seed =
            await SeedCheckoutDataAsync(
                "Store A");

        var order =
            CreateOrder(
                seed);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            tenantContext.Orders.Add(
                order);

            await tenantContext.SaveChangesAsync();
        }

        /*
         * Simulate catalog changes after checkout.
         *
         * We intentionally modify the source rows
         * directly so this test proves that the Order
         * snapshot does not depend on the live catalog.
         */
        await using (var catalogUpdateContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            await catalogUpdateContext.Database
                .ExecuteSqlInterpolatedAsync(
                    $"""
                    UPDATE catalog_products
                    SET
                        name = {"Changed Product Name"},
                        price_amount = {999m}
                    WHERE
                        tenant_id = {seed.TenantId.Value}
                        AND id = {seed.ProductId.Value};
                    """);

            await catalogUpdateContext.Database
                .ExecuteSqlInterpolatedAsync(
                    $"""
                    UPDATE catalog_product_variants
                    SET
                        name = {"Changed Variant Name"},
                        sku = {"CHANGED-SKU"},
                        price_override_amount = {777m}
                    WHERE
                        tenant_id = {seed.TenantId.Value}
                        AND id = {seed.VariantId.Value};
                    """);
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var saved =
            await verificationContext
                .Orders
                .Include("_items")
                .SingleAsync(
                    item =>
                        item.Id ==
                        order.Id);

        var savedItem =
            Assert.Single(
                saved.Items);

        Assert.Equal(
            seed.ProductName,
            savedItem.ProductName);

        Assert.Equal(
            seed.VariantName,
            savedItem.VariantName);

        Assert.Equal(
            seed.Sku,
            savedItem.Sku);

        Assert.Equal(
            30m,
            savedItem.UnitPrice.Amount);

        Assert.Equal(
            2,
            savedItem.Quantity);

        Assert.Equal(
            60m,
            savedItem.LineTotal);

        Assert.Equal(
            60m,
            saved.TotalAmount);
    }

    private async Task<SeededCheckoutData>
        SeedCheckoutDataAsync(
            string tenantName)
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                tenantName,
                $"order-store-{Guid.NewGuid():N}",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.Add(
                tenant);

            await setupContext.SaveChangesAsync();
        }

        const string productName =
            "Checkout Product";

        var product =
            Product.Create(
                tenant.Id,
                productName,
                $"checkout-product-{Guid.NewGuid():N}",
                Money.Create(
                    25m,
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

        const string variantName =
            "Black / M";

        var sku =
            $"ORDER-{Guid.NewGuid():N}";

        var variant =
            ProductVariant.Create(
                tenant.Id,
                product.Id,
                variantName,
                ProductSku.Create(
                    sku),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 20,
                    lowStockThreshold: 3),
                now,
                priceOverride:
                    Money.Create(
                        30m,
                        "USD"),
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
                now);

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

        return new SeededCheckoutData(
            tenant.Id,
            customerUserId,
            cart.Id,
            product.Id,
            variant.Id,
            productName,
            variantName,
            sku);
    }

    private static Order CreateOrder(
        SeededCheckoutData seed)
    {
        return Order.Create(
            seed.TenantId,
            seed.CustomerUserId,
            seed.CartId,
            CurrencyCode.Create(
                "USD"),
            new[]
            {
                CreateSnapshot(
                    seed)
            },
            DateTimeOffset.UtcNow,
            seed.CustomerUserId.Value);
    }

    private static OrderItemSnapshot CreateSnapshot(
        SeededCheckoutData seed)
    {
        return new OrderItemSnapshot(
            seed.ProductId,
            seed.VariantId,
            seed.ProductName,
            seed.VariantName,
            seed.Sku,
            Money.Create(
                30m,
                "USD"),
            2);
    }

    private sealed record SeededCheckoutData(
        TenantId TenantId,
        UserId CustomerUserId,
        CartId CartId,
        ProductId ProductId,
        ProductVariantId VariantId,
        string ProductName,
        string VariantName,
        string Sku);
}