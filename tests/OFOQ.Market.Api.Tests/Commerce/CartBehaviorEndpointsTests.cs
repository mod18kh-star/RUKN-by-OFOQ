using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Commerce.Carts;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Commerce;

public sealed class CartBehaviorEndpointsTests
{
    [Fact]
    public async Task AddItem_UsesVariantPriceOverride()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var catalog =
            CreateProductAndVariant(
                factory,
                setup.Tenant,
                productPrice: 25m,
                variantPriceOverride: 30m,
                quantity: 20);

        var response =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    setup.Tenant.Id),
                new AddToCartRequest(
                    catalog.Product.Id.Value,
                    catalog.Variant.Id.Value,
                    2));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var cart =
            await response.Content
                .ReadFromJsonAsync<
                    CartResponse>();

        Assert.NotNull(
            cart);

        Assert.Equal(
            "USD",
            cart.Currency);

        Assert.Equal(
            2,
            cart.TotalQuantity);

        Assert.Equal(
            60m,
            cart.TotalAmount);

        var item =
            Assert.Single(
                cart.Items);

        Assert.Equal(
            30m,
            item.UnitPrice);

        Assert.Equal(
            2,
            item.Quantity);

        Assert.Equal(
            60m,
            item.LineTotal);
    }

    [Fact]
    public async Task AddItem_TwiceForSameVariant_AggregatesQuantity()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var catalog =
            CreateProductAndVariant(
                factory,
                setup.Tenant,
                productPrice: 15m,
                variantPriceOverride: null,
                quantity: 20);

        var firstResponse =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    setup.Tenant.Id),
                new AddToCartRequest(
                    catalog.Product.Id.Value,
                    catalog.Variant.Id.Value,
                    2));

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var secondResponse =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    setup.Tenant.Id),
                new AddToCartRequest(
                    catalog.Product.Id.Value,
                    catalog.Variant.Id.Value,
                    3));

        Assert.Equal(
            HttpStatusCode.OK,
            secondResponse.StatusCode);

        var cart =
            await secondResponse.Content
                .ReadFromJsonAsync<
                    CartResponse>();

        Assert.NotNull(
            cart);

        Assert.Equal(
            5,
            cart.TotalQuantity);

        Assert.Equal(
            75m,
            cart.TotalAmount);

        var item =
            Assert.Single(
                cart.Items);

        Assert.Equal(
            5,
            item.Quantity);

        Assert.Equal(
            15m,
            item.UnitPrice);
    }

    [Fact]
    public async Task GetCart_AfterAddingItem_ReturnsActiveCart()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var catalog =
            CreateProductAndVariant(
                factory,
                setup.Tenant,
                productPrice: 40m,
                variantPriceOverride: null,
                quantity: 10);

        var addResponse =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    setup.Tenant.Id),
                new AddToCartRequest(
                    catalog.Product.Id.Value,
                    catalog.Variant.Id.Value,
                    2));

        Assert.Equal(
            HttpStatusCode.OK,
            addResponse.StatusCode);

        var getResponse =
            await client.GetAsync(
                CartUrl(
                    setup.Tenant.Id));

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var cart =
            await getResponse.Content
                .ReadFromJsonAsync<
                    CartResponse>();

        Assert.NotNull(
            cart);

        Assert.Equal(
            2,
            cart.TotalQuantity);

        Assert.Equal(
            80m,
            cart.TotalAmount);

        var item =
            Assert.Single(
                cart.Items);

        Assert.Equal(
            catalog.Product.Id.Value,
            item.ProductId);

        Assert.Equal(
            catalog.Variant.Id.Value,
            item.ProductVariantId);
    }

    [Fact]
    public async Task AddItem_WhenStockIsInsufficient_ReturnsConflict()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var catalog =
            CreateProductAndVariant(
                factory,
                setup.Tenant,
                productPrice: 10m,
                variantPriceOverride: null,
                quantity: 2);

        var response =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    setup.Tenant.Id),
                new AddToCartRequest(
                    catalog.Product.Id.Value,
                    catalog.Variant.Id.Value,
                    3));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<
                    ErrorResponse>();

        Assert.NotNull(
            error);

        Assert.Equal(
            "cart_insufficient_stock",
            error.Code);
    }

    [Fact]
    public async Task AddItem_WhenContinueSellingOutOfStockIsEnabled_Succeeds()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var catalog =
            CreateProductAndVariant(
                factory,
                setup.Tenant,
                productPrice: 12m,
                variantPriceOverride: null,
                quantity: 0,
                continueSellingWhenOutOfStock: true);

        var response =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    setup.Tenant.Id),
                new AddToCartRequest(
                    catalog.Product.Id.Value,
                    catalog.Variant.Id.Value,
                    5));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var cart =
            await response.Content
                .ReadFromJsonAsync<
                    CartResponse>();

        Assert.NotNull(
            cart);

        Assert.Equal(
            5,
            cart.TotalQuantity);

        Assert.Equal(
            60m,
            cart.TotalAmount);
    }

    [Fact]
    public async Task AddItem_WhenVariantBelongsToDifferentProduct_ReturnsNotFound()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var now =
            DateTimeOffset.UtcNow;

        var productA =
            Product.Create(
                setup.Tenant.Id,
                "Product A",
                $"product-a-{Guid.NewGuid():N}",
                Money.Create(
                    10m,
                    "USD"),
                now);

        productA.Publish(
            now.AddSeconds(1));

        var productB =
            Product.Create(
                setup.Tenant.Id,
                "Product B",
                $"product-b-{Guid.NewGuid():N}",
                Money.Create(
                    20m,
                    "USD"),
                now);

        productB.Publish(
            now.AddSeconds(1));

        var variantB =
            ProductVariant.Create(
                setup.Tenant.Id,
                productB.Id,
                "Product B Default",
                ProductSku.Create(
                    $"B-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 10),
                now,
                isDefault:
                    true);

        AddProduct(
            factory,
            productA);

        AddProduct(
            factory,
            productB);

        AddVariant(
            factory,
            variantB);

        var response =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    setup.Tenant.Id),
                new AddToCartRequest(
                    productA.Id.Value,
                    variantB.Id.Value,
                    1));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<
                    ErrorResponse>();

        Assert.NotNull(
            error);

        Assert.Equal(
            "cart_variant_not_available",
            error.Code);
    }

    [Fact]
    public async Task AddItem_WithProductFromAnotherTenant_ReturnsNotFound()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var user =
            await CreateUserAsync(
                factory,
                client);

        var tenantA =
            await CreateTenantAsync(
                factory,
                "Store A");

        var tenantB =
            await CreateTenantAsync(
                factory,
                "Store B");

        SetAccessToken(
            factory,
            client,
            user);

        var catalogB =
            CreateProductAndVariant(
                factory,
                tenantB,
                productPrice: 20m,
                variantPriceOverride: null,
                quantity: 10);

        var response =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    tenantA.Id),
                new AddToCartRequest(
                    catalogB.Product.Id.Value,
                    catalogB.Variant.Id.Value,
                    1));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<
                    ErrorResponse>();

        Assert.NotNull(
            error);

        Assert.Equal(
            "cart_product_not_available",
            error.Code);
    }

    [Fact]
    public async Task AddItem_WithUnpublishedProduct_ReturnsNotFound()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var now =
            DateTimeOffset.UtcNow;

        var product =
            Product.Create(
                setup.Tenant.Id,
                "Draft Product",
                $"draft-product-{Guid.NewGuid():N}",
                Money.Create(
                    10m,
                    "USD"),
                now);

        var variant =
            ProductVariant.Create(
                setup.Tenant.Id,
                product.Id,
                "Default",
                ProductSku.Create(
                    $"D-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 10),
                now,
                isDefault:
                    true);

        AddProduct(
            factory,
            product);

        AddVariant(
            factory,
            variant);

        var response =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    setup.Tenant.Id),
                new AddToCartRequest(
                    product.Id.Value,
                    variant.Id.Value,
                    1));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<
                    ErrorResponse>();

        Assert.NotNull(
            error);

        Assert.Equal(
            "cart_product_not_available",
            error.Code);
    }

    private static ProductSetup CreateProductAndVariant(
        MarketApiFactory factory,
        Tenant tenant,
        decimal productPrice,
        decimal? variantPriceOverride,
        int quantity,
        bool continueSellingWhenOutOfStock = false)
    {
        var now =
            DateTimeOffset.UtcNow;

        var product =
            Product.Create(
                tenant.Id,
                "Cart Product",
                $"cart-product-{Guid.NewGuid():N}",
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
                    $"SKU-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: quantity,
                    continueSellingWhenOutOfStock:
                        continueSellingWhenOutOfStock),
                now,
                priceOverride:
                    variantPriceOverride.HasValue
                        ? Money.Create(
                            variantPriceOverride.Value,
                            "USD")
                        : null,
                isDefault:
                    true);

        AddProduct(
            factory,
            product);

        AddVariant(
            factory,
            variant);

        return new ProductSetup(
            product,
            variant);
    }

    private static void AddProduct(
        MarketApiFactory factory,
        Product product)
    {
        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryProductStore>();

        lock (store.SyncRoot)
        {
            store.Items.Add(
                product);
        }
    }

    private static void AddVariant(
        MarketApiFactory factory,
        ProductVariant variant)
    {
        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryProductVariantStore>();

        lock (store.SyncRoot)
        {
            store.Items.Add(
                variant);
        }
    }

    private static async Task<AuthSetup>
        CreateAuthenticatedSetupAsync(
            MarketApiFactory factory,
            HttpClient client)
    {
        var user =
            await CreateUserAsync(
                factory,
                client);

        var tenant =
            await CreateTenantAsync(
                factory,
                "Cart Store");

        SetAccessToken(
            factory,
            client,
            user);

        return new AuthSetup(
            user,
            tenant);
    }

    private static async Task<User>
        CreateUserAsync(
            MarketApiFactory factory,
            HttpClient client)
    {
        var email =
            $"cart-{Guid.NewGuid():N}@example.com";

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    email,
                    "StrongPassword123"));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    RegisterUserResponse>();

        Assert.NotNull(
            result);

        var repository =
            factory.Services
                .GetRequiredService<
                    IUserRepository>();

        var user =
            await repository.GetByIdAsync(
                UserId.From(
                    result.UserId));

        Assert.NotNull(
            user);

        return user;
    }

    private static async Task<Tenant>
        CreateTenantAsync(
            MarketApiFactory factory,
            string name)
    {
        var tenant =
            Tenant.Create(
                name,
                $"cart-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow);

        var repository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await repository.AddAsync(
            tenant);

        return tenant;
    }

    private static void SetAccessToken(
        MarketApiFactory factory,
        HttpClient client,
        User user)
    {
        var service =
            factory.Services
                .GetRequiredService<
                    IAccessTokenService>();

        var token =
            service.Create(
                user.Id,
                user.Email.Value,
                DateTimeOffset.UtcNow,
                AccessTokenAuthenticationLevel.PasswordOnly);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token.Token);
    }

    private static string CartUrl(
        TenantId tenantId)
    {
        return
            $"/api/tenants/{tenantId.Value}/cart";
    }

    private static string ItemsUrl(
        TenantId tenantId)
    {
        return
            $"{CartUrl(tenantId)}/items";
    }

    private sealed record ProductSetup(
        Product Product,
        ProductVariant Variant);

    private sealed record AuthSetup(
        User User,
        Tenant Tenant);

    private sealed record ErrorResponse(
        string Code,
        string Message);
}