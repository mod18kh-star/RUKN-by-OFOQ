using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Catalog;

public sealed class ProductPersistenceTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Product_RoundTrip_PreservesPricingAndCatalogData()
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

        var category =
            Category.Create(
                tenant.Id,
                "Phones",
                "phones",
                now);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Categories.Add(
                category);

            await tenantContext.SaveChangesAsync();
        }

        var product =
            Product.Create(
                tenant.Id,
                "iPhone 17 Pro",
                "iphone-17-pro",
                Money.Create(
                    999m,
                    "USD"),
                now,
                categoryId:
                    category.Id,
                description:
                    "Flagship smartphone",
                compareAtPrice:
                    Money.Create(
                        1099m,
                        "USD"));

        product.Publish(
            now.AddMinutes(1));

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Products.Add(
                product);

            await tenantContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        var saved =
            await verificationContext
                .Products
                .SingleAsync(
                    item =>
                        item.Id ==
                        product.Id);

        Assert.Equal(
            "iPhone 17 Pro",
            saved.Name);

        Assert.Equal(
            "iphone-17-pro",
            saved.Slug);

        Assert.Equal(
            category.Id,
            saved.CategoryId);

        Assert.Equal(
            "Flagship smartphone",
            saved.Description);

        Assert.Equal(
            999m,
            saved.Price.Amount);

        Assert.Equal(
            "USD",
            saved.Price.Currency.Value);

        Assert.NotNull(
            saved.CompareAtPrice);

        Assert.Equal(
            1099m,
            saved.CompareAtPrice!.Value.Amount);

        Assert.Equal(
            ProductStatus.Published,
            saved.Status);

        Assert.True(
            saved.IsVisible);
    }

    [Fact]
    public async Task ProductQuery_ReturnsOnlyCurrentTenantProducts()
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

        await using (var contextA =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantA.Id)))
        {
            contextA.Products.Add(
                Product.Create(
                    tenantA.Id,
                    "Product A",
                    "product-a",
                    Money.Create(
                        10m,
                        "USD"),
                    now));

            await contextA.SaveChangesAsync();
        }

        await using (var contextB =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantB.Id)))
        {
            contextB.Products.Add(
                Product.Create(
                    tenantB.Id,
                    "Product B",
                    "product-b",
                    Money.Create(
                        20m,
                        "USD"),
                    now));

            await contextB.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantA.Id));

        var products =
            await verificationContext
                .Products
                .ToListAsync();

        var product =
            Assert.Single(
                products);

        Assert.Equal(
            tenantA.Id,
            product.TenantId);

        Assert.Equal(
            "Product A",
            product.Name);
    }

    [Fact]
    public async Task Database_RejectsCrossTenantCategoryReference()
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

        var categoryB =
            Category.Create(
                tenantB.Id,
                "Category B",
                "category-b",
                now);

        await using (var contextB =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantB.Id)))
        {
            contextB.Categories.Add(
                categoryB);

            await contextB.SaveChangesAsync();
        }

        await using var contextA =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantA.Id));

        contextA.Products.Add(
            Product.Create(
                tenantA.Id,
                "Illegal Product",
                "illegal-product",
                Money.Create(
                    10m,
                    "USD"),
                now,
                categoryId:
                    categoryB.Id));

        await Assert.ThrowsAsync<
            DbUpdateException>(
                () =>
                    contextA.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_EnforcesProductSlugUniquenessWithinTenant()
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

        await using var tenantContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        tenantContext.Products.AddRange(
            Product.Create(
                tenant.Id,
                "Product One",
                "same-slug",
                Money.Create(
                    10m,
                    "USD"),
                now),

            Product.Create(
                tenant.Id,
                "Product Two",
                "same-slug",
                Money.Create(
                    20m,
                    "USD"),
                now));

        await Assert.ThrowsAsync<
            DbUpdateException>(
                () =>
                    tenantContext
                        .SaveChangesAsync());
    }

    [Fact]
    public async Task SameProductSlug_IsAllowedAcrossDifferentTenants()
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

        await using (var contextA =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantA.Id)))
        {
            contextA.Products.Add(
                Product.Create(
                    tenantA.Id,
                    "Product A",
                    "shared-slug",
                    Money.Create(
                        10m,
                        "USD"),
                    now));

            await contextA.SaveChangesAsync();
        }

        await using (var contextB =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantB.Id)))
        {
            contextB.Products.Add(
                Product.Create(
                    tenantB.Id,
                    "Product B",
                    "shared-slug",
                    Money.Create(
                        20m,
                        "USD"),
                    now));

            await contextB.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Variant_RoundTrip_PreservesSkuPricingAndInventory()
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
                    quantity: 12,
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

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        var saved =
            await verificationContext
                .ProductVariants
                .SingleAsync(
                    item =>
                        item.Id ==
                        variant.Id);

        Assert.Equal(
            "SHIRT-BLACK-M",
            saved.Sku.Value);

        Assert.True(
            saved.IsDefault);

        Assert.NotNull(
            saved.PriceOverride);

        Assert.Equal(
            30m,
            saved.PriceOverride!.Value.Amount);

        Assert.Equal(
            "USD",
            saved.PriceOverride!.Value.Currency.Value);

        Assert.True(
            saved.Inventory.TrackInventory);

        Assert.Equal(
            12,
            saved.Inventory.Quantity);

        Assert.Equal(
            3,
            saved.Inventory.LowStockThreshold);
    }

    [Fact]
    public async Task Database_EnforcesSkuUniquenessWithinTenant()
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
                    20m,
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

        await using var context =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        context.ProductVariants.AddRange(
            ProductVariant.Create(
                tenant.Id,
                product.Id,
                "Black",
                ProductSku.Create(
                    "same-sku"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true),
                now),

            ProductVariant.Create(
                tenant.Id,
                product.Id,
                "White",
                ProductSku.Create(
                    "same-sku"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true),
                now));

        await Assert.ThrowsAsync<
            DbUpdateException>(
                () =>
                    context
                        .SaveChangesAsync());
    }

    [Fact]
    public async Task Database_AllowsSameSkuAcrossDifferentTenants()
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

        var productA =
            Product.Create(
                tenantA.Id,
                "Product A",
                "product-a",
                Money.Create(
                    10m,
                    "USD"),
                now);

        var productB =
            Product.Create(
                tenantB.Id,
                "Product B",
                "product-b",
                Money.Create(
                    10m,
                    "USD"),
                now);

        await using (var contextA =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantA.Id)))
        {
            contextA.Products.Add(
                productA);

            await contextA.SaveChangesAsync();

            contextA.ProductVariants.Add(
                ProductVariant.Create(
                    tenantA.Id,
                    productA.Id,
                    "Default",
                    ProductSku.Create(
                        "shared-sku"),
                    CurrencyCode.Create(
                        "USD"),
                    Inventory.Create(
                        trackInventory: true),
                    now));

            await contextA.SaveChangesAsync();
        }

        await using (var contextB =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantB.Id)))
        {
            contextB.Products.Add(
                productB);

            await contextB.SaveChangesAsync();

            contextB.ProductVariants.Add(
                ProductVariant.Create(
                    tenantB.Id,
                    productB.Id,
                    "Default",
                    ProductSku.Create(
                        "shared-sku"),
                    CurrencyCode.Create(
                        "USD"),
                    Inventory.Create(
                        trackInventory: true),
                    now));

            await contextB.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Database_RejectsVariantReferencingProductFromAnotherTenant()
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

        await using var contextA =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantA.Id));

        contextA.ProductVariants.Add(
            ProductVariant.Create(
                tenantA.Id,
                productB.Id,
                "Illegal Variant",
                ProductSku.Create(
                    "illegal-variant"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true),
                now));

        await Assert.ThrowsAsync<
            DbUpdateException>(
                () =>
                    contextA
                        .SaveChangesAsync());
    }

    [Fact]
    public async Task Database_AllowsOnlyOneActiveDefaultVariantPerProduct()
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
                    20m,
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

        await using var context =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        context.ProductVariants.AddRange(
            ProductVariant.Create(
                tenant.Id,
                product.Id,
                "Default One",
                ProductSku.Create(
                    "default-one"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true),
                now,
                isDefault:
                    true),

            ProductVariant.Create(
                tenant.Id,
                product.Id,
                "Default Two",
                ProductSku.Create(
                    "default-two"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true),
                now,
                isDefault:
                    true));

        await Assert.ThrowsAsync<
            DbUpdateException>(
                () =>
                    context
                        .SaveChangesAsync());
    }
}