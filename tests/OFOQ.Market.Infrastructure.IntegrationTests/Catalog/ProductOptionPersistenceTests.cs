using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Catalog;

public sealed class ProductOptionPersistenceTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Option_RoundTrip_PreservesData()
    {
        await _database.ResetAsync();

        var fixture =
            await CreateFixtureAsync(
                "Store A",
                "store-a");

        var now =
            DateTimeOffset.UtcNow;

        var option =
            ProductOption.Create(
                fixture.Tenant.Id,
                fixture.Product.Id,
                "Color",
                0,
                now);

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             fixture.Tenant.Id)))
        {
            context.ProductOptions.Add(
                option);

            await context.SaveChangesAsync();
        }

        await using var verification =
            _database.CreateContext(
                new TestCurrentTenant(
                    fixture.Tenant.Id));

        var saved =
            await verification.ProductOptions
                .SingleAsync();

        Assert.Equal(
            "Color",
            saved.Name);

        Assert.Equal(
            "color",
            saved.NormalizedName);

        Assert.Equal(
            fixture.Product.Id,
            saved.ProductId);
    }

    [Fact]
    public async Task Database_RejectsDuplicateOptionNameWithinProduct()
    {
        await _database.ResetAsync();

        var fixture =
            await CreateFixtureAsync(
                "Store A",
                "store-a");

        await using var context =
            _database.CreateContext(
                new TestCurrentTenant(
                    fixture.Tenant.Id));

        context.ProductOptions.AddRange(
            ProductOption.Create(
                fixture.Tenant.Id,
                fixture.Product.Id,
                "Color",
                0,
                DateTimeOffset.UtcNow),

            ProductOption.Create(
                fixture.Tenant.Id,
                fixture.Product.Id,
                "COLOR",
                1,
                DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                context.SaveChangesAsync());
    }

    [Fact]
    public async Task SameOptionName_IsAllowedOnDifferentProducts()
    {
        await _database.ResetAsync();

        var fixture =
            await CreateFixtureAsync(
                "Store A",
                "store-a");

        var productTwo =
            Product.Create(
                fixture.Tenant.Id,
                "Product Two",
                "product-two",
                Money.Create(
                    10m,
                    "USD"),
                DateTimeOffset.UtcNow);

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             fixture.Tenant.Id)))
        {
            context.Products.Add(
                productTwo);

            await context.SaveChangesAsync();

            context.ProductOptions.AddRange(
                ProductOption.Create(
                    fixture.Tenant.Id,
                    fixture.Product.Id,
                    "Color",
                    0,
                    DateTimeOffset.UtcNow),

                ProductOption.Create(
                    fixture.Tenant.Id,
                    productTwo.Id,
                    "Color",
                    0,
                    DateTimeOffset.UtcNow));

            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Database_RejectsCrossTenantProductOption()
    {
        await _database.ResetAsync();

        var fixtureA =
            await CreateFixtureAsync(
                "Store A",
                "store-a");

        var fixtureB =
            await CreateFixtureAsync(
                "Store B",
                "store-b");

        await using var contextA =
            _database.CreateContext(
                new TestCurrentTenant(
                    fixtureA.Tenant.Id));

        contextA.ProductOptions.Add(
            ProductOption.Create(
                fixtureA.Tenant.Id,
                fixtureB.Product.Id,
                "Illegal",
                0,
                DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                contextA.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_RejectsDuplicateValueWithinOption()
    {
        await _database.ResetAsync();

        var fixture =
            await CreateFixtureAsync(
                "Store A",
                "store-a");

        var option =
            await CreateOptionAsync(
                fixture,
                "Color");

        await using var context =
            _database.CreateContext(
                new TestCurrentTenant(
                    fixture.Tenant.Id));

        context.ProductOptionValues.AddRange(
            ProductOptionValue.Create(
                fixture.Tenant.Id,
                fixture.Product.Id,
                option.Id,
                "Black",
                0,
                DateTimeOffset.UtcNow),

            ProductOptionValue.Create(
                fixture.Tenant.Id,
                fixture.Product.Id,
                option.Id,
                "BLACK",
                1,
                DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                context.SaveChangesAsync());
    }

    [Fact]
    public async Task SameValue_IsAllowedAcrossDifferentOptions()
    {
        await _database.ResetAsync();

        var fixture =
            await CreateFixtureAsync(
                "Store A",
                "store-a");

        var color =
            await CreateOptionAsync(
                fixture,
                "Color");

        var finish =
            await CreateOptionAsync(
                fixture,
                "Finish",
                1);

        await using var context =
            _database.CreateContext(
                new TestCurrentTenant(
                    fixture.Tenant.Id));

        context.ProductOptionValues.AddRange(
            ProductOptionValue.Create(
                fixture.Tenant.Id,
                fixture.Product.Id,
                color.Id,
                "Black",
                0,
                DateTimeOffset.UtcNow),

            ProductOptionValue.Create(
                fixture.Tenant.Id,
                fixture.Product.Id,
                finish.Id,
                "Black",
                0,
                DateTimeOffset.UtcNow));

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task VariantAssignment_RoundTrip_PreservesRelationships()
    {
        await _database.ResetAsync();

        var fixture =
            await CreateFixtureAsync(
                "Store A",
                "store-a");

        var option =
            await CreateOptionAsync(
                fixture,
                "Color");

        var value =
            await CreateOptionValueAsync(
                fixture,
                option,
                "Black");

        var assignment =
            ProductVariantOptionValue.Create(
                fixture.Tenant.Id,
                fixture.Product.Id,
                fixture.Variant.Id,
                option.Id,
                value.Id,
                DateTimeOffset.UtcNow);

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             fixture.Tenant.Id)))
        {
            context.ProductVariantOptionValues.Add(
                assignment);

            await context.SaveChangesAsync();
        }

        await using var verification =
            _database.CreateContext(
                new TestCurrentTenant(
                    fixture.Tenant.Id));

        var saved =
            await verification
                .ProductVariantOptionValues
                .SingleAsync();

        Assert.Equal(
            fixture.Variant.Id,
            saved.ProductVariantId);

        Assert.Equal(
            option.Id,
            saved.ProductOptionId);

        Assert.Equal(
            value.Id,
            saved.ProductOptionValueId);
    }

    [Fact]
    public async Task Database_RejectsValueFromAnotherProduct()
    {
        await _database.ResetAsync();

        var fixtureA =
            await CreateFixtureAsync(
                "Store A",
                "store-a");

        var productB =
            Product.Create(
                fixtureA.Tenant.Id,
                "Product B",
                "product-b",
                Money.Create(
                    10m,
                    "USD"),
                DateTimeOffset.UtcNow);

        ProductVariant variantB;

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             fixtureA.Tenant.Id)))
        {
            context.Products.Add(
                productB);

            await context.SaveChangesAsync();

            variantB =
                ProductVariant.Create(
                    fixtureA.Tenant.Id,
                    productB.Id,
                    "Default",
                    ProductSku.Create(
                        "PRODUCT-B-SKU"),
                    CurrencyCode.Create(
                        "USD"),
                    Inventory.Create(
                        true,
                        10),
                    DateTimeOffset.UtcNow,
                    isDefault: true);

            context.ProductVariants.Add(
                variantB);

            await context.SaveChangesAsync();
        }

        var optionA =
            await CreateOptionAsync(
                fixtureA,
                "Color");

        var fixtureB =
            new CatalogFixture(
                fixtureA.Tenant,
                productB,
                variantB);

        var optionB =
            await CreateOptionAsync(
                fixtureB,
                "Size");

        var valueB =
            await CreateOptionValueAsync(
                fixtureB,
                optionB,
                "Large");

        await using var assignmentContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    fixtureA.Tenant.Id));

        assignmentContext
            .ProductVariantOptionValues
            .Add(
                ProductVariantOptionValue.Create(
                    fixtureA.Tenant.Id,
                    fixtureA.Product.Id,
                    fixtureA.Variant.Id,
                    optionA.Id,
                    valueB.Id,
                    DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                assignmentContext
                    .SaveChangesAsync());
    }

    [Fact]
    public async Task Database_AllowsOnlyOneValuePerOptionPerVariant()
    {
        await _database.ResetAsync();

        var fixture =
            await CreateFixtureAsync(
                "Store A",
                "store-a");

        var option =
            await CreateOptionAsync(
                fixture,
                "Color");

        var black =
            await CreateOptionValueAsync(
                fixture,
                option,
                "Black");

        var white =
            await CreateOptionValueAsync(
                fixture,
                option,
                "White",
                1);

        await using var context =
            _database.CreateContext(
                new TestCurrentTenant(
                    fixture.Tenant.Id));

        context.ProductVariantOptionValues.AddRange(
            ProductVariantOptionValue.Create(
                fixture.Tenant.Id,
                fixture.Product.Id,
                fixture.Variant.Id,
                option.Id,
                black.Id,
                DateTimeOffset.UtcNow),

            ProductVariantOptionValue.Create(
                fixture.Tenant.Id,
                fixture.Product.Id,
                fixture.Variant.Id,
                option.Id,
                white.Id,
                DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                context.SaveChangesAsync());
    }

    [Fact]
    public async Task NoTenantContext_ReturnsNoStructuredVariantData()
    {
        await _database.ResetAsync();

        var fixture =
            await CreateFixtureAsync(
                "Store A",
                "store-a");

        var option =
            await CreateOptionAsync(
                fixture,
                "Color");

        var value =
            await CreateOptionValueAsync(
                fixture,
                option,
                "Black");

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             fixture.Tenant.Id)))
        {
            tenantContext
                .ProductVariantOptionValues
                .Add(
                    ProductVariantOptionValue.Create(
                        fixture.Tenant.Id,
                        fixture.Product.Id,
                        fixture.Variant.Id,
                        option.Id,
                        value.Id,
                        DateTimeOffset.UtcNow));

            await tenantContext.SaveChangesAsync();
        }

        await using var noTenantContext =
            _database.CreateContext();

        Assert.Empty(
            await noTenantContext
                .ProductOptions
                .ToListAsync());

        Assert.Empty(
            await noTenantContext
                .ProductOptionValues
                .ToListAsync());

        Assert.Empty(
            await noTenantContext
                .ProductVariantOptionValues
                .ToListAsync());
    }

    private async Task<CatalogFixture>
        CreateFixtureAsync(
            string tenantName,
            string tenantSlug)
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                tenantName,
                tenantSlug,
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
                $"{tenantName} Product",
                $"{tenantSlug}-product",
                Money.Create(
                    100m,
                    "USD"),
                now);

        ProductVariant variant;

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Products.Add(
                product);

            await tenantContext.SaveChangesAsync();

            variant =
                ProductVariant.Create(
                    tenant.Id,
                    product.Id,
                    "Default",
                    ProductSku.Create(
                        $"{tenantSlug}-sku"),
                    CurrencyCode.Create(
                        "USD"),
                    Inventory.Create(
                        true,
                        10),
                    now,
                    isDefault: true);

            tenantContext.ProductVariants.Add(
                variant);

            await tenantContext.SaveChangesAsync();
        }

        return new CatalogFixture(
            tenant,
            product,
            variant);
    }

    private async Task<ProductOption>
        CreateOptionAsync(
            CatalogFixture fixture,
            string name,
            int sortOrder = 0)
    {
        var option =
            ProductOption.Create(
                fixture.Tenant.Id,
                fixture.Product.Id,
                name,
                sortOrder,
                DateTimeOffset.UtcNow);

        await using var context =
            _database.CreateContext(
                new TestCurrentTenant(
                    fixture.Tenant.Id));

        context.ProductOptions.Add(
            option);

        await context.SaveChangesAsync();

        return option;
    }

    private async Task<ProductOptionValue>
        CreateOptionValueAsync(
            CatalogFixture fixture,
            ProductOption option,
            string value,
            int sortOrder = 0)
    {
        var optionValue =
            ProductOptionValue.Create(
                fixture.Tenant.Id,
                fixture.Product.Id,
                option.Id,
                value,
                sortOrder,
                DateTimeOffset.UtcNow);

        await using var context =
            _database.CreateContext(
                new TestCurrentTenant(
                    fixture.Tenant.Id));

        context.ProductOptionValues.Add(
            optionValue);

        await context.SaveChangesAsync();

        return optionValue;
    }

    private sealed record CatalogFixture(
        Tenant Tenant,
        Product Product,
        ProductVariant Variant);
}