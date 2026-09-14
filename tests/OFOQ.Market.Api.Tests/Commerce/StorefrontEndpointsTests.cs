using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Contracts.Storefront;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Catalog.Attributes;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Commerce;

public sealed class StorefrontEndpointsTests
{
    [Fact]
    public async Task Storefront_IsPublic_AndReturnsActiveStore()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateTenantAsync(
                factory,
                active: true);

        SeedPrimaryVertical(
            factory,
            tenant,
            CommerceVerticalType.MobilePhones);

        var response =
            await client.GetAsync(
                StoreUrl(
                    tenant));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    StorefrontInfoResponse>();

        Assert.NotNull(
            result);

        Assert.Equal(
            tenant.Name,
            result.Name);

        Assert.Equal(
            tenant.Slug.Value,
            result.Slug);

        Assert.Equal(
            nameof(
                CommerceVerticalType.MobilePhones),
            result.Vertical);
    }

    [Fact]
    public async Task Storefront_DraftOrSuspendedStore_ReturnsNotFound()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var draft =
            await CreateTenantAsync(
                factory,
                active: false);

        var draftResponse =
            await client.GetAsync(
                StoreUrl(
                    draft));

        Assert.Equal(
            HttpStatusCode.NotFound,
            draftResponse.StatusCode);

        var suspended =
            await CreateTenantAsync(
                factory,
                active: true);

        suspended.Suspend(
            DateTimeOffset.UtcNow);

        var suspendedResponse =
            await client.GetAsync(
                StoreUrl(
                    suspended));

        Assert.Equal(
            HttpStatusCode.NotFound,
            suspendedResponse.StatusCode);
    }

    [Fact]
    public async Task Products_ExposeOnlyPublishedVisibleTenantProducts()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateTenantAsync(
                factory,
                active: true);

        var otherTenant =
            await CreateTenantAsync(
                factory,
                active: true);

        var category =
            SeedCategory(
                factory,
                tenant,
                "Phones",
                "phones");

        var visible =
            SeedProduct(
                factory,
                tenant,
                "Alpha Phone",
                "alpha-phone",
                category.Id,
                publish: true);

        SeedVariant(
            factory,
            visible,
            tenant,
            quantity: 5);

        SeedProduct(
            factory,
            tenant,
            "Draft Phone",
            "draft-phone",
            category.Id,
            publish: false);

        var hidden =
            SeedProduct(
                factory,
                tenant,
                "Hidden Phone",
                "hidden-phone",
                category.Id,
                publish: true);

        hidden.Hide(
            DateTimeOffset.UtcNow);

        SeedProduct(
            factory,
            otherTenant,
            "Other Tenant Phone",
            "other-phone",
            null,
            publish: true);

        var response =
            await client.GetAsync(
                $"{StoreUrl(tenant)}/products");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    StorefrontProductPageResponse>();

        Assert.NotNull(
            result);

        var item =
            Assert.Single(
                result.Items);

        Assert.Equal(
            "alpha-phone",
            item.Slug);

        Assert.True(
            item.AvailableForSale);
    }

    [Fact]
    public async Task Products_SupportSearchCategoryAndPagination()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateTenantAsync(
                factory,
                active: true);

        var phones =
            SeedCategory(
                factory,
                tenant,
                "Phones",
                "phones");

        var accessories =
            SeedCategory(
                factory,
                tenant,
                "Accessories",
                "accessories");

        var alpha =
            SeedProduct(
                factory,
                tenant,
                "Alpha Phone",
                "alpha-phone",
                phones.Id,
                publish: true,
                createdAtOffsetMinutes: 3);

        var beta =
            SeedProduct(
                factory,
                tenant,
                "Beta Phone",
                "beta-phone",
                phones.Id,
                publish: true,
                createdAtOffsetMinutes: 2);

        SeedProduct(
            factory,
            tenant,
            "Phone Case",
            "phone-case",
            accessories.Id,
            publish: true,
            createdAtOffsetMinutes: 1);

        SeedVariant(
            factory,
            alpha,
            tenant,
            quantity: 1);

        SeedVariant(
            factory,
            beta,
            tenant,
            quantity: 1);

        var response =
            await client.GetAsync(
                $"{StoreUrl(tenant)}/products" +
                "?search=phone&category=phones&page=1&pageSize=1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    StorefrontProductPageResponse>();

        Assert.NotNull(
            result);

        Assert.Equal(
            2,
            result.TotalCount);

        Assert.Equal(
            2,
            result.TotalPages);

        Assert.Single(
            result.Items);
    }

    [Fact]
    public async Task ProductDetail_ReturnsVariantsAvailabilityAndAttributes()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateTenantAsync(
                factory,
                active: true);

        SeedPrimaryVertical(
            factory,
            tenant,
            CommerceVerticalType.MobilePhones);

        var category =
            SeedCategory(
                factory,
                tenant,
                "Phones",
                "phones");

        var product =
            SeedProduct(
                factory,
                tenant,
                "Alpha Phone",
                "alpha-phone",
                category.Id,
                publish: true);

        SeedVariant(
            factory,
            product,
            tenant,
            quantity: 5,
            priceOverride: 950m,
            isDefault: true);

        SeedVariant(
            factory,
            product,
            tenant,
            quantity: 0,
            priceOverride: null,
            isDefault: false);

        var disabled =
            SeedVariant(
                factory,
                product,
                tenant,
                quantity: 10,
                priceOverride: null,
                isDefault: false);

        disabled.Disable(
            DateTimeOffset.UtcNow);

        SeedAttribute(
            factory,
            tenant,
            product,
            "brand",
            "Apple");

        SeedAttribute(
            factory,
            tenant,
            product,
            "storage-gb",
            "256");

        var response =
            await client.GetAsync(
                $"{StoreUrl(tenant)}/products/alpha-phone");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    StorefrontProductDetailResponse>();

        Assert.NotNull(
            result);

        Assert.Equal(
            "Phones",
            result.CategoryName);

        Assert.True(
            result.AvailableForSale);

        Assert.Equal(
            2,
            result.Variants.Count);

        var defaultVariant =
            Assert.Single(
                result.Variants,
                item =>
                    item.IsDefault);

        Assert.Equal(
            950m,
            defaultVariant.Price);

        Assert.Equal(
            5,
            defaultVariant.Quantity);

        Assert.Contains(
            result.Attributes,
            item =>
                item.Key ==
                    "brand" &&
                item.Value ==
                    "Apple");
    }

    [Fact]
    public async Task ProductDetail_DraftOrHiddenProduct_ReturnsNotFound()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateTenantAsync(
                factory,
                active: true);

        SeedProduct(
            factory,
            tenant,
            "Draft",
            "draft-product",
            null,
            publish: false);

        var hidden =
            SeedProduct(
                factory,
                tenant,
                "Hidden",
                "hidden-product",
                null,
                publish: true);

        hidden.Hide(
            DateTimeOffset.UtcNow);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync(
                $"{StoreUrl(tenant)}/products/draft-product"))
                .StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync(
                $"{StoreUrl(tenant)}/products/hidden-product"))
                .StatusCode);
    }

    [Fact]
    public async Task Products_InvalidPagination_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateTenantAsync(
                factory,
                active: true);

        var response =
            await client.GetAsync(
                $"{StoreUrl(tenant)}/products?page=0&pageSize=500");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private static async Task<Tenant> CreateTenantAsync(
        MarketApiFactory factory,
        bool active)
    {
        var tenant =
            Tenant.Create(
                "Storefront Test",
                $"store-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow);

        if (active)
        {
            tenant.Activate(
                DateTimeOffset.UtcNow);
        }

        var repository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await repository.AddAsync(
            tenant);

        return tenant;
    }

    private static Category SeedCategory(
        MarketApiFactory factory,
        Tenant tenant,
        string name,
        string slug)
    {
        var category =
            Category.Create(
                tenant.Id,
                name,
                slug,
                DateTimeOffset.UtcNow);

        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryCategoryStore>();

        lock (store.SyncRoot)
        {
            store.Items.Add(
                category);
        }

        return category;
    }

    private static Product SeedProduct(
        MarketApiFactory factory,
        Tenant tenant,
        string name,
        string slug,
        CategoryId? categoryId,
        bool publish,
        int createdAtOffsetMinutes = 0,
        bool seedImages = true)
    {
        var now =
            DateTimeOffset.UtcNow
                .AddMinutes(
                    createdAtOffsetMinutes);

        var product =
            Product.Create(
                tenant.Id,
                name,
                slug,
                Money.Create(
                    1000m,
                    "USD"),
                now,
                categoryId:
                    categoryId);

        if (publish)
        {
            product.Publish(
                now.AddSeconds(1));
        }

        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryProductStore>();

        lock (store.SyncRoot)
        {
            store.Items.Add(
                product);
        }

        if (publish &&
            seedImages)
        {
            SeedImages(
                factory,
                tenant,
                product);
        }

        return product;
    }

    private static void SeedImages(
        MarketApiFactory factory,
        Tenant tenant,
        Product product)
    {
        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryProductImageStore>();

        var now =
            DateTimeOffset.UtcNow;

        var primary =
            ProductImage.Create(
                tenant.Id,
                product.Id,
                $"https://images.example.test/{product.Slug}-primary.jpg",
                $"{product.Name} primary image",
                0,
                true,
                now);

        var secondary =
            ProductImage.Create(
                tenant.Id,
                product.Id,
                $"https://images.example.test/{product.Slug}-secondary.jpg",
                $"{product.Name} secondary image",
                1,
                false,
                now);

        lock (store.SyncRoot)
        {
            store.Items.Add(
                primary);

            store.Items.Add(
                secondary);
        }
    }
    private static ProductVariant SeedVariant(
        MarketApiFactory factory,
        Product product,
        Tenant tenant,
        int quantity,
        decimal? priceOverride = null,
        bool isDefault = false)
    {
        var variant =
            ProductVariant.Create(
                tenant.Id,
                product.Id,
                isDefault
                    ? "Default"
                    : $"Variant-{Guid.NewGuid():N}",
                ProductSku.Create(
                    $"SF-{Guid.NewGuid():N}"),
                product.Price.Currency,
                Inventory.Create(
                    trackInventory: true,
                    quantity: quantity,
                    lowStockThreshold: 1),
                DateTimeOffset.UtcNow,
                priceOverride:
                    priceOverride.HasValue
                        ? Money.Create(
                            priceOverride.Value,
                            product.Price.Currency)
                        : null,
                isDefault:
                    isDefault);

        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryProductVariantStore>();

        lock (store.SyncRoot)
        {
            store.Items.Add(
                variant);
        }

        return variant;
    }

    private static void SeedPrimaryVertical(
        MarketApiFactory factory,
        Tenant tenant,
        CommerceVerticalType type)
    {
        var vertical =
            TenantCommerceVertical.Create(
                tenant.Id,
                type,
                isPrimary: true,
                DateTimeOffset.UtcNow);

        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryTenantCommerceVerticalStore>();

        lock (store.SyncRoot)
        {
            store.Items.Add(
                vertical);
        }
    }

    private static void SeedAttribute(
        MarketApiFactory factory,
        Tenant tenant,
        Product product,
        string key,
        string value)
    {
        var attribute =
            ProductAttributeValue.Create(
                tenant.Id,
                product.Id,
                key,
                value,
                DateTimeOffset.UtcNow);

        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryProductAttributeValueStore>();

        lock (store.SyncRoot)
        {
            store.Items.Add(
                attribute);
        }
    }

    private static string StoreUrl(
        Tenant tenant)
    {
        return
            $"/api/storefront/{tenant.Slug.Value}";
    }
}