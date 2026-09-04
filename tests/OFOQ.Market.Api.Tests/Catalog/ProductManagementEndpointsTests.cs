using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Catalog;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Catalog;

public sealed class ProductManagementEndpointsTests
{
    private const string Email =
        "product-management-owner@example.com";

    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task UpdateProduct_MfaOwner_UpdatesProductAndDefaultSku()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        var product =
            await CreateProductAsync(
                client,
                setup.Tenant.Id,
                name: "Old Product",
                slug: "old-product",
                sku: "OLD-SKU");

        var request =
            new UpdateProductRequest(
                "Updated Product",
                "UPDATED-PRODUCT",
                "Updated description",
                null,
                150m,
                190m,
                "sar",
                "updated-sku");

        var response =
            await client.PutAsJsonAsync(
                ProductUrl(
                    setup.Tenant.Id,
                    product.ProductId),
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await response.Content
                .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(
            updated);

        Assert.Equal(
            "Updated Product",
            updated.Name);

        Assert.Equal(
            "updated-product",
            updated.Slug);

        Assert.Equal(
            "Updated description",
            updated.Description);

        Assert.Equal(
            150m,
            updated.Price);

        Assert.Equal(
            190m,
            updated.CompareAtPrice);

        Assert.Equal(
            "SAR",
            updated.Currency);

        var defaultVariant =
            Assert.Single(
                updated.Variants);

        Assert.Equal(
            "UPDATED-SKU",
            defaultVariant.Sku);

        /*
         * Updating product identity/pricing must not
         * accidentally reset existing inventory.
         */
        Assert.Equal(
            10,
            defaultVariant.Quantity);

        Assert.True(
            defaultVariant.TrackInventory);
    }

    [Fact]
    public async Task UpdateProduct_PasswordOnlyOwner_ReturnsForbidden()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var user =
            await CreateUserAsync(
                factory,
                client);

        var tenant =
            await CreateTenantAsync(
                factory,
                user,
                "Store One",
                "store-one");

        SetAccessToken(
            factory,
            client,
            user,
            AccessTokenAuthenticationLevel.MultiFactor);

        var product =
            await CreateProductAsync(
                client,
                tenant.Id);

        SetAccessToken(
            factory,
            client,
            user,
            AccessTokenAuthenticationLevel.PasswordOnly);

        var response =
            await client.PutAsJsonAsync(
                ProductUrl(
                    tenant.Id,
                    product.ProductId),
                ValidUpdateRequest());

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_DuplicateSlug_ReturnsConflict()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        await CreateProductAsync(
            client,
            setup.Tenant.Id,
            name: "Product One",
            slug: "product-one",
            sku: "SKU-ONE");

        var productTwo =
            await CreateProductAsync(
                client,
                setup.Tenant.Id,
                name: "Product Two",
                slug: "product-two",
                sku: "SKU-TWO");

        var request =
            new UpdateProductRequest(
                "Product Two",
                "product-one",
                null,
                null,
                100m,
                null,
                "USD",
                "SKU-TWO");

        var response =
            await client.PutAsJsonAsync(
                ProductUrl(
                    setup.Tenant.Id,
                    productTwo.ProductId),
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        Assert.Equal(
            "product_slug_already_exists",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task UpdateProduct_DuplicateSku_ReturnsConflict()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        await CreateProductAsync(
            client,
            setup.Tenant.Id,
            name: "Product One",
            slug: "product-one",
            sku: "SKU-ONE");

        var productTwo =
            await CreateProductAsync(
                client,
                setup.Tenant.Id,
                name: "Product Two",
                slug: "product-two",
                sku: "SKU-TWO");

        var request =
            new UpdateProductRequest(
                "Product Two",
                "product-two",
                null,
                null,
                100m,
                null,
                "USD",
                "sku-one");

        var response =
            await client.PutAsJsonAsync(
                ProductUrl(
                    setup.Tenant.Id,
                    productTwo.ProductId),
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        Assert.Equal(
            "product_sku_already_exists",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task UpdateProduct_CategoryFromAnotherTenant_ReturnsBadRequest()
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
                user,
                "Store A",
                "store-a");

        var tenantB =
            await CreateTenantAsync(
                factory,
                user,
                "Store B",
                "store-b");

        SetAccessToken(
            factory,
            client,
            user,
            AccessTokenAuthenticationLevel.MultiFactor);

        var product =
            await CreateProductAsync(
                client,
                tenantA.Id);

        var categoryB =
            Category.Create(
                tenantB.Id,
                "Foreign Category",
                "foreign-category",
                DateTimeOffset.UtcNow);

        var categoryStore =
            factory.Services
                .GetRequiredService<
                    InMemoryCategoryStore>();

        lock (categoryStore.SyncRoot)
        {
            categoryStore.Items.Add(
                categoryB);
        }

        var request =
            new UpdateProductRequest(
                "Product",
                "product",
                null,
                categoryB.Id.Value,
                100m,
                null,
                "USD",
                "TEST-SKU");

        var response =
            await client.PutAsJsonAsync(
                ProductUrl(
                    tenantA.Id,
                    product.ProductId),
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "product_category_not_found",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task UpdateProduct_FromAnotherTenant_ReturnsNotFound()
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
                user,
                "Store A",
                "store-a");

        var tenantB =
            await CreateTenantAsync(
                factory,
                user,
                "Store B",
                "store-b");

        SetAccessToken(
            factory,
            client,
            user,
            AccessTokenAuthenticationLevel.MultiFactor);

        var productB =
            await CreateProductAsync(
                client,
                tenantB.Id,
                name: "Product B",
                slug: "product-b",
                sku: "SKU-B");

        var response =
            await client.PutAsJsonAsync(
                ProductUrl(
                    tenantA.Id,
                    productB.ProductId),
                ValidUpdateRequest());

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "product_not_found",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task PublishProduct_SetsPublishedAndVisible()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        var product =
            await CreateProductAsync(
                client,
                setup.Tenant.Id);

        var response =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    product.ProductId,
                    "publish"),
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await response.Content
                .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(
            updated);

        Assert.Equal(
            "Published",
            updated.Status);

        Assert.True(
            updated.IsVisible);
    }

    [Fact]
    public async Task HideThenShowProduct_WorksForPublishedProduct()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        var product =
            await CreateProductAsync(
                client,
                setup.Tenant.Id);

        var publish =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    product.ProductId,
                    "publish"),
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            publish.StatusCode);

        var hide =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    product.ProductId,
                    "hide"),
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            hide.StatusCode);

        var hiddenProduct =
            await hide.Content
                .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(
            hiddenProduct);

        Assert.Equal(
            "Published",
            hiddenProduct.Status);

        Assert.False(
            hiddenProduct.IsVisible);

        var show =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    product.ProductId,
                    "show"),
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            show.StatusCode);

        var visibleProduct =
            await show.Content
                .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(
            visibleProduct);

        Assert.Equal(
            "Published",
            visibleProduct.Status);

        Assert.True(
            visibleProduct.IsVisible);
    }

    [Fact]
    public async Task ShowProduct_WhileDraft_ReturnsConflict()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        var product =
            await CreateProductAsync(
                client,
                setup.Tenant.Id);

        Assert.Equal(
            "Draft",
            product.Status);

        var response =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    product.ProductId,
                    "show"),
                null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        Assert.Equal(
            "invalid_product_state",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task MoveProductToDraft_HidesPublishedProduct()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        var product =
            await CreateProductAsync(
                client,
                setup.Tenant.Id);

        var publish =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    product.ProductId,
                    "publish"),
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            publish.StatusCode);

        var response =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    product.ProductId,
                    "draft"),
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await response.Content
                .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(
            updated);

        Assert.Equal(
            "Draft",
            updated.Status);

        Assert.False(
            updated.IsVisible);
    }

    [Fact]
    public async Task ArchiveProduct_HidesAndArchivesProduct()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        var product =
            await CreateProductAsync(
                client,
                setup.Tenant.Id);

        var publish =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    product.ProductId,
                    "publish"),
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            publish.StatusCode);

        var response =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    product.ProductId,
                    "archive"),
                null);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await response.Content
                .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(
            updated);

        Assert.Equal(
            "Archived",
            updated.Status);

        Assert.False(
            updated.IsVisible);
    }

    [Fact]
    public async Task UpdateInventory_UpdatesDefaultVariantInventory()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        var product =
            await CreateProductAsync(
                client,
                setup.Tenant.Id);

        var request =
            new UpdateProductInventoryRequest(
                true,
                3,
                5,
                true);

        var response =
            await client.PutAsJsonAsync(
                InventoryUrl(
                    setup.Tenant.Id,
                    product.ProductId),
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var updated =
            await response.Content
                .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(
            updated);

        var variant =
            Assert.Single(
                updated.Variants);

        Assert.True(
            variant.TrackInventory);

        Assert.Equal(
            3,
            variant.Quantity);

        Assert.Equal(
            5,
            variant.LowStockThreshold);

        Assert.True(
            variant.ContinueSellingWhenOutOfStock);

        Assert.True(
            variant.IsAvailableForSale);
    }

    [Fact]
    public async Task UpdateInventory_NegativeQuantity_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        var product =
            await CreateProductAsync(
                client,
                setup.Tenant.Id);

        var request =
            new UpdateProductInventoryRequest(
                true,
                -5,
                2,
                false);

        var response =
            await client.PutAsJsonAsync(
                InventoryUrl(
                    setup.Tenant.Id,
                    product.ProductId),
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "validation_error",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task UpdateInventory_FromAnotherTenant_ReturnsNotFound()
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
                user,
                "Store A",
                "store-a");

        var tenantB =
            await CreateTenantAsync(
                factory,
                user,
                "Store B",
                "store-b");

        SetAccessToken(
            factory,
            client,
            user,
            AccessTokenAuthenticationLevel.MultiFactor);

        var productB =
            await CreateProductAsync(
                client,
                tenantB.Id,
                name: "Product B",
                slug: "product-b",
                sku: "SKU-B");

        var response =
            await client.PutAsJsonAsync(
                InventoryUrl(
                    tenantA.Id,
                    productB.ProductId),
                new UpdateProductInventoryRequest(
                    true,
                    50,
                    5,
                    false));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "product_not_found",
            await ReadErrorCodeAsync(
                response));
    }

    private static async Task<TestSetup>
        CreateMfaSetupAsync(
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
                user,
                "Product Management Store",
                "product-management-store");

        SetAccessToken(
            factory,
            client,
            user,
            AccessTokenAuthenticationLevel.MultiFactor);

        return new TestSetup(
            user,
            tenant);
    }

    private static async Task<User>
        CreateUserAsync(
            MarketApiFactory factory,
            HttpClient client)
    {
        var registrationResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    Email,
                    Password));

        Assert.Equal(
            HttpStatusCode.Created,
            registrationResponse.StatusCode);

        var registration =
            await registrationResponse.Content
                .ReadFromJsonAsync<RegisterUserResponse>();

        Assert.NotNull(
            registration);

        var userRepository =
            factory.Services
                .GetRequiredService<IUserRepository>();

        var user =
            await userRepository
                .GetByIdAsync(
                    UserId.From(
                        registration.UserId));

        Assert.NotNull(
            user);

        return user;
    }

    private static async Task<Tenant>
        CreateTenantAsync(
            MarketApiFactory factory,
            User user,
            string name,
            string slug)
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                name,
                slug,
                now,
                user.Id.Value);

        var tenantRepository =
            factory.Services
                .GetRequiredService<ITenantRepository>();

        await tenantRepository
            .AddAsync(
                tenant);

        var membership =
            TenantMembership.Create(
                tenant.Id,
                user.Id,
                TenantRole.Owner,
                now,
                user.Id.Value);

        var membershipRepository =
            factory.Services
                .GetRequiredService<
                    ITenantMembershipRepository>();

        await membershipRepository
            .AddAsync(
                membership);

        return tenant;
    }

    private static async Task<ProductResponse>
        CreateProductAsync(
            HttpClient client,
            TenantId tenantId,
            string name = "Test Product",
            string slug = "test-product",
            string sku = "TEST-SKU")
    {
        var response =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    tenantId),
                new CreateProductRequest(
                    name,
                    slug,
                    "Initial description",
                    null,
                    100m,
                    120m,
                    "USD",
                    sku,
                    true,
                    10,
                    2,
                    false));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var product =
            await response.Content
                .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(
            product);

        return product;
    }

    private static void SetAccessToken(
        MarketApiFactory factory,
        HttpClient client,
        User user,
        AccessTokenAuthenticationLevel authenticationLevel)
    {
        var accessTokenService =
            factory.Services
                .GetRequiredService<IAccessTokenService>();

        var accessToken =
            accessTokenService.Create(
                user.Id,
                user.Email.Value,
                DateTimeOffset.UtcNow,
                authenticationLevel);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken.Token);
    }

    private static UpdateProductRequest
        ValidUpdateRequest()
    {
        return new UpdateProductRequest(
            "Updated Product",
            "updated-product",
            "Updated description",
            null,
            150m,
            180m,
            "USD",
            "UPDATED-SKU");
    }

    private static string ProductsUrl(
        TenantId tenantId)
    {
        return
            $"/api/tenants/{tenantId.Value}/backoffice/products";
    }

    private static string ProductUrl(
        TenantId tenantId,
        Guid productId)
    {
        return
            $"{ProductsUrl(tenantId)}/{productId}";
    }

    private static string InventoryUrl(
        TenantId tenantId,
        Guid productId)
    {
        return
            $"{ProductUrl(tenantId, productId)}/inventory";
    }

    private static string StateUrl(
        TenantId tenantId,
        Guid productId,
        string action)
    {
        return
            $"{ProductUrl(tenantId, productId)}/{action}";
    }

    private static async Task<string?>
        ReadErrorCodeAsync(
            HttpResponseMessage response)
    {
        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return json.RootElement
            .GetProperty("code")
            .GetString();
    }

    private sealed record TestSetup(
        User User,
        Tenant Tenant);
}