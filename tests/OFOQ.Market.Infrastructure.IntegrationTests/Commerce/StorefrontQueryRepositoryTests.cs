using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Catalog.Attributes;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence.Repositories;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class StorefrontQueryRepositoryTests
{
    private readonly IntegrationTestDatabase
        _database =
            IntegrationTestDatabase.Create();

    [Fact]
    public async Task Storefront_ReadsOnlyPublicTenantScopedCatalog()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenantA =
            Tenant.Create(
                "Public Store",
                $"public-store-{Guid.NewGuid():N}",
                now);

        tenantA.Activate(
            now);

        var tenantB =
            Tenant.Create(
                "Other Store",
                $"other-store-{Guid.NewGuid():N}",
                now);

        tenantB.Activate(
            now);

        var suspended =
            Tenant.Create(
                "Suspended Store",
                $"suspended-store-{Guid.NewGuid():N}",
                now);

        suspended.Activate(
            now);

        suspended.Suspend(
            now.AddMinutes(1));

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.AddRange(
                tenantA,
                tenantB,
                suspended);

            await setupContext.SaveChangesAsync();
        }

        var phones =
            Category.Create(
                tenantA.Id,
                "Phones",
                "phones",
                now);

        var hiddenCategory =
            Category.Create(
                tenantA.Id,
                "Hidden",
                "hidden",
                now,
                sortOrder: 20);

        hiddenCategory.Hide(
            now.AddMinutes(1));

        var visible =
            Product.Create(
                tenantA.Id,
                "Alpha Phone",
                "alpha-phone",
                Money.Create(
                    1000m,
                    "USD"),
                now,
                categoryId:
                    phones.Id,
                description:
                    "Published phone");

        visible.Publish(
            now.AddMinutes(1));

        var draft =
            Product.Create(
                tenantA.Id,
                "Draft Phone",
                "draft-phone",
                Money.Create(
                    500m,
                    "USD"),
                now.AddMinutes(2),
                categoryId:
                    phones.Id);

        var hidden =
            Product.Create(
                tenantA.Id,
                "Hidden Phone",
                "hidden-phone",
                Money.Create(
                    600m,
                    "USD"),
                now.AddMinutes(3),
                categoryId:
                    phones.Id);

        hidden.Publish(
            now.AddMinutes(4));

        hidden.Hide(
            now.AddMinutes(5));

        var archived =
            Product.Create(
                tenantA.Id,
                "Archived Phone",
                "archived-phone",
                Money.Create(
                    700m,
                    "USD"),
                now.AddMinutes(6),
                categoryId:
                    phones.Id);

        archived.Publish(
            now.AddMinutes(7));

        archived.Archive(
            now.AddMinutes(8));

        var deleted =
            Product.Create(
                tenantA.Id,
                "Deleted Phone",
                "deleted-phone",
                Money.Create(
                    800m,
                    "USD"),
                now.AddMinutes(9),
                categoryId:
                    phones.Id);

        deleted.Publish(
            now.AddMinutes(10));

        deleted.Delete(
            now.AddMinutes(11));

        var otherTenantProduct =
            Product.Create(
                tenantB.Id,
                "Other Tenant Phone",
                "other-tenant-phone",
                Money.Create(
                    9000m,
                    "USD"),
                now,
                categoryId:
                    null);

        otherTenantProduct.Publish(
            now.AddMinutes(1));

        var primaryVariant =
            ProductVariant.Create(
                tenantA.Id,
                visible.Id,
                "256 GB",
                ProductSku.Create(
                    $"SF-{Guid.NewGuid():N}"),
                visible.Price.Currency,
                Inventory.Create(
                    true,
                    5,
                    1),
                now,
                priceOverride:
                    Money.Create(
                        950m,
                        "USD"),
                isDefault:
                    true);

        var outOfStock =
            ProductVariant.Create(
                tenantA.Id,
                visible.Id,
                "512 GB",
                ProductSku.Create(
                    $"SF-{Guid.NewGuid():N}"),
                visible.Price.Currency,
                Inventory.Create(
                    true,
                    0,
                    1),
                now.AddMinutes(1));

        var disabledVariant =
            ProductVariant.Create(
                tenantA.Id,
                visible.Id,
                "Disabled",
                ProductSku.Create(
                    $"SF-{Guid.NewGuid():N}"),
                visible.Price.Currency,
                Inventory.Create(
                    true,
                    10,
                    1),
                now.AddMinutes(2));

        disabledVariant.Disable(
            now.AddMinutes(3));

        var primaryImage =
            ProductImage.Create(
                tenantA.Id,
                visible.Id,
                "https://images.example.test/alpha-primary.jpg",
                "Alpha Phone",
                0,
                true,
                now);

        var secondaryImage =
            ProductImage.Create(
                tenantA.Id,
                visible.Id,
                "https://images.example.test/alpha-secondary.jpg",
                "Alpha Phone alternate",
                1,
                false,
                now);
        var vertical =
            TenantCommerceVertical.Create(
                tenantA.Id,
                CommerceVerticalType.MobilePhones,
                isPrimary: true,
                now);

        var brand =
            ProductAttributeValue.Create(
                tenantA.Id,
                visible.Id,
                "brand",
                "Apple",
                now);

        var storage =
            ProductAttributeValue.Create(
                tenantA.Id,
                visible.Id,
                "storage-gb",
                "256",
                now);

        await using (var tenantAContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantA.Id)))
        {
            tenantAContext.Categories.AddRange(
                phones,
                hiddenCategory);

            tenantAContext.Products.AddRange(
                visible,
                draft,
                hidden,
                archived,
                deleted);

            tenantAContext.ProductVariants.AddRange(
                primaryVariant,
                outOfStock,
                disabledVariant);
            tenantAContext.ProductImages.AddRange(
                primaryImage,
                secondaryImage);

            tenantAContext.TenantCommerceVerticals.Add(
                vertical);

            tenantAContext.ProductAttributeValues.AddRange(
                brand,
                storage);

            await tenantAContext.SaveChangesAsync();
        }

        await using (var tenantBContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantB.Id)))
        {
            tenantBContext.Products.Add(
                otherTenantProduct);

            await tenantBContext.SaveChangesAsync();
        }

        await using var readContext =
            _database.CreateContext();

        var repository =
            new StorefrontQueryRepository(
                readContext);

        var store =
            await repository.GetStoreAsync(
                tenantA.Slug);

        Assert.NotNull(
            store);

        Assert.Equal(
            tenantA.Id,
            store.TenantId);

        Assert.Equal(
            nameof(
                CommerceVerticalType.MobilePhones),
            store.Vertical);

        var suspendedResult =
            await repository.GetStoreAsync(
                suspended.Slug);

        Assert.Null(
            suspendedResult);

        var categories =
            await repository.GetCategoriesAsync(
                tenantA.Id);

        var category =
            Assert.Single(
                categories);

        Assert.Equal(
            "phones",
            category.Slug);

        var products =
            await repository.GetProductsAsync(
                tenantA.Id,
                search: null,
                categorySlug: null,
                page: 1,
                pageSize: 10);

        var product =
            Assert.Single(
                products.Items);

        Assert.Equal(
            visible.Id.Value,
            product.ProductId);

        Assert.True(
            product.AvailableForSale);

        var filtered =
            await repository.GetProductsAsync(
                tenantA.Id,
                search: "alpha",
                categorySlug: "phones",
                page: 1,
                pageSize: 10);

        Assert.Single(
            filtered.Items);

        var wrongCategory =
            await repository.GetProductsAsync(
                tenantA.Id,
                search: null,
                categorySlug: "missing",
                page: 1,
                pageSize: 10);

        Assert.Empty(
            wrongCategory.Items);

        var detail =
            await repository.GetProductBySlugAsync(
                tenantA.Id,
                "alpha-phone");

        Assert.NotNull(
            detail);

        Assert.Equal(
            "Phones",
            detail.CategoryName);

        Assert.True(
            detail.AvailableForSale);

        Assert.Equal(
            2,
            detail.Variants.Count);

        var defaultVariant =
            Assert.Single(
                detail.Variants,
                item =>
                    item.IsDefault);

        Assert.Equal(
            950m,
            defaultVariant.Price);

        Assert.Equal(
            5,
            defaultVariant.Quantity);

        var unavailableVariant =
            Assert.Single(
                detail.Variants,
                item =>
                    !item.AvailableForSale);

        Assert.Equal(
            0,
            unavailableVariant.Quantity);

        Assert.Equal(
            2,
            detail.Attributes.Count);

        Assert.Contains(
            detail.Attributes,
            item =>
                item.Key ==
                    "brand" &&
                item.Value ==
                    "Apple");

        Assert.Null(
            await repository.GetProductBySlugAsync(
                tenantA.Id,
                "draft-phone"));

        Assert.Null(
            await repository.GetProductBySlugAsync(
                tenantA.Id,
                "hidden-phone"));

        Assert.DoesNotContain(
            products.Items,
            item =>
                item.Slug ==
                    "other-tenant-phone");
    }
}