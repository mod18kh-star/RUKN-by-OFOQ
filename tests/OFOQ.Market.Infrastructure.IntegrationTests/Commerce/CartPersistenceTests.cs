using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class CartPersistenceTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Cart_RoundTrip_PreservesItemsPricingAndCustomer()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Store A",
                "store-a",
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
                "T-Shirt",
                "t-shirt",
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

        var variant =
            ProductVariant.Create(
                tenant.Id,
                product.Id,
                "Black / M",
                ProductSku.Create(
                    "shirt-black-m"),
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
            now);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Carts.Add(
                cart);

            await tenantContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        var saved =
            await verificationContext
                .Carts
                .Include("_items")
                .SingleAsync(
                    item =>
                        item.Id ==
                        cart.Id);

        Assert.Equal(
            tenant.Id,
            saved.TenantId);

        Assert.Equal(
            customerUserId,
            saved.CustomerUserId);

        Assert.Equal(
            CartStatus.Active,
            saved.Status);

        Assert.NotNull(
            saved.Currency);

        Assert.Equal(
            "USD",
            saved.Currency!.Value.Value);

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
            product.Id,
            item.ProductId);

        Assert.Equal(
            variant.Id,
            item.ProductVariantId);

        Assert.Equal(
            2,
            item.Quantity);

        Assert.Equal(
            30m,
            item.UnitPrice.Amount);

        Assert.Equal(
            "USD",
            item.UnitPrice.Currency.Value);

        Assert.Equal(
            60m,
            item.LineTotal);
    }

    [Fact]
    public async Task CartQuery_ReturnsOnlyCurrentTenantCarts()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenantA =
            Tenant.Create(
                "Store A",
                "store-a",
                now);

        var tenantB =
            Tenant.Create(
                "Store B",
                "store-b",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.AddRange(
                tenantA,
                tenantB);

            await setupContext.SaveChangesAsync();
        }

        var customerA =
            UserId.New();

        var customerB =
            UserId.New();

        await using (var contextA =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantA.Id)))
        {
            contextA.Carts.Add(
                Cart.Create(
                    tenantA.Id,
                    customerA,
                    now));

            await contextA.SaveChangesAsync();
        }

        await using (var contextB =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantB.Id)))
        {
            contextB.Carts.Add(
                Cart.Create(
                    tenantB.Id,
                    customerB,
                    now));

            await contextB.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantA.Id));

        var carts =
            await verificationContext
                .Carts
                .ToListAsync();

        var cart =
            Assert.Single(
                carts);

        Assert.Equal(
            tenantA.Id,
            cart.TenantId);

        Assert.Equal(
            customerA,
            cart.CustomerUserId);
    }

    [Fact]
    public async Task Query_WithoutTenantContext_ReturnsNoCartsOrCartItems()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Store A",
                "store-a",
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
                "Product",
                "product",
                Money.Create(
                    10m,
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
                    "product-default"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 10),
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

        var cart =
            Cart.Create(
                tenant.Id,
                UserId.New(),
                now);

        cart.AddItem(
            product.Id,
            variant.Id,
            Money.Create(
                10m,
                "USD"),
            1,
            now);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Carts.Add(
                cart);

            await tenantContext.SaveChangesAsync();
        }

        await using var noTenantContext =
            _database.CreateContext();

        var carts =
            await noTenantContext
                .Carts
                .ToListAsync();

        var cartItems =
            await noTenantContext
                .CartItems
                .ToListAsync();

        Assert.Empty(
            carts);

        Assert.Empty(
            cartItems);
    }

    [Fact]
    public async Task Database_AllowsSameCustomerActiveCartAcrossDifferentTenants()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenantA =
            Tenant.Create(
                "Store A",
                "store-a",
                now);

        var tenantB =
            Tenant.Create(
                "Store B",
                "store-b",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.AddRange(
                tenantA,
                tenantB);

            await setupContext.SaveChangesAsync();
        }

        var customerUserId =
            UserId.New();

        await using (var contextA =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantA.Id)))
        {
            contextA.Carts.Add(
                Cart.Create(
                    tenantA.Id,
                    customerUserId,
                    now));

            await contextA.SaveChangesAsync();
        }

        await using (var contextB =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantB.Id)))
        {
            contextB.Carts.Add(
                Cart.Create(
                    tenantB.Id,
                    customerUserId,
                    now));

            await contextB.SaveChangesAsync();
        }

        await using var verificationA =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantA.Id));

        await using var verificationB =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantB.Id));

        Assert.Single(
            await verificationA
                .Carts
                .ToListAsync());

        Assert.Single(
            await verificationB
                .Carts
                .ToListAsync());
    }

    [Fact]
    public async Task Database_RejectsTwoActiveCartsForSameCustomerWithinTenant()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Store A",
                "store-a",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.Add(
                tenant);

            await setupContext.SaveChangesAsync();
        }

        var customerUserId =
            UserId.New();

        await using var tenantContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        tenantContext.Carts.AddRange(
            Cart.Create(
                tenant.Id,
                customerUserId,
                now),

            Cart.Create(
                tenant.Id,
                customerUserId,
                now.AddSeconds(1)));

        await Assert.ThrowsAsync<
            DbUpdateException>(
                () =>
                    tenantContext
                        .SaveChangesAsync());
    }

    [Fact]
    public async Task Database_RejectsCartItemReferencingProductFromAnotherTenant()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenantA =
            Tenant.Create(
                "Store A",
                "store-a",
                now);

        var tenantB =
            Tenant.Create(
                "Store B",
                "store-b",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.AddRange(
                tenantA,
                tenantB);

            await setupContext.SaveChangesAsync();
        }

        var productB =
            Product.Create(
                tenantB.Id,
                "Product B",
                "product-b",
                Money.Create(
                    20m,
                    "USD"),
                now);

        await using (var contextB =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantB.Id)))
        {
            contextB.Products.Add(
                productB);

            await contextB.SaveChangesAsync();
        }

        var variantB =
            ProductVariant.Create(
                tenantB.Id,
                productB.Id,
                "Default",
                ProductSku.Create(
                    "product-b-default"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 10),
                now,
                isDefault:
                    true);

        await using (var contextB =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantB.Id)))
        {
            contextB.ProductVariants.Add(
                variantB);

            await contextB.SaveChangesAsync();
        }

        var illegalCart =
            Cart.Create(
                tenantA.Id,
                UserId.New(),
                now);

        illegalCart.AddItem(
            productB.Id,
            variantB.Id,
            Money.Create(
                20m,
                "USD"),
            1,
            now);

        await using var contextA =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantA.Id));

        contextA.Carts.Add(
            illegalCart);

        await Assert.ThrowsAsync<
            DbUpdateException>(
                () =>
                    contextA
                        .SaveChangesAsync());
    }

    [Fact]
    public async Task Database_RejectsVariantThatBelongsToDifferentProduct()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Store A",
                "store-a",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.Add(
                tenant);

            await setupContext.SaveChangesAsync();
        }

        var productA =
            Product.Create(
                tenant.Id,
                "Product A",
                "product-a",
                Money.Create(
                    10m,
                    "USD"),
                now);

        var productB =
            Product.Create(
                tenant.Id,
                "Product B",
                "product-b",
                Money.Create(
                    20m,
                    "USD"),
                now);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Products.AddRange(
                productA,
                productB);

            await tenantContext.SaveChangesAsync();
        }

        var variantB =
            ProductVariant.Create(
                tenant.Id,
                productB.Id,
                "Product B Default",
                ProductSku.Create(
                    "product-b-variant"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 10),
                now,
                isDefault:
                    true);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.ProductVariants.Add(
                variantB);

            await tenantContext.SaveChangesAsync();
        }

        var cart =
            Cart.Create(
                tenant.Id,
                UserId.New(),
                now);

        /*
         * Product A + Variant B is intentionally invalid.
         *
         * The Cart aggregate can hold IDs, but PostgreSQL
         * must reject this because the Variant belongs
         * to Product B.
         */
        cart.AddItem(
            productA.Id,
            variantB.Id,
            Money.Create(
                10m,
                "USD"),
            1,
            now);

await using var invalidCartContext =
    _database.CreateContext(
        new TestCurrentTenant(
            tenant.Id));

invalidCartContext.Carts.Add(
    cart);

await Assert.ThrowsAsync<
    DbUpdateException>(
        () =>
            invalidCartContext
                .SaveChangesAsync());
    }

    [Fact]
    public async Task Database_AllowsNewActiveCartAfterPreviousCartConverted()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Store A",
                "store-a",
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
                "Product",
                "product",
                Money.Create(
                    10m,
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
                    "product-default"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 10),
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

        var firstCart =
            Cart.Create(
                tenant.Id,
                customerUserId,
                now);

        firstCart.AddItem(
            product.Id,
            variant.Id,
            Money.Create(
                10m,
                "USD"),
            1,
            now);

        firstCart.MarkConverted(
            now.AddMinutes(1));

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Carts.Add(
                firstCart);

            await tenantContext.SaveChangesAsync();
        }

        var secondCart =
            Cart.Create(
                tenant.Id,
                customerUserId,
                now.AddMinutes(2));

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Carts.Add(
                secondCart);

            await tenantContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        var carts =
            await verificationContext
                .Carts
                .OrderBy(
                    item =>
                        item.CreatedAtUtc)
                .ToListAsync();

        Assert.Equal(
            2,
            carts.Count);

        Assert.Contains(
            carts,
            item =>
                item.Status ==
                CartStatus.Converted);

        Assert.Contains(
            carts,
            item =>
                item.Status ==
                CartStatus.Active);
    }
}