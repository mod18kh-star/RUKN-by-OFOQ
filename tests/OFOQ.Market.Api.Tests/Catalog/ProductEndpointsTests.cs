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

public sealed class ProductEndpointsTests
{
    private const string Email =
        "products-owner@example.com";

    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task Products_WithoutAccessToken_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            Tenant.Create(
                "Store One",
                "store-one",
                DateTimeOffset.UtcNow);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await tenantRepository
            .AddAsync(
                tenant);

        var response =
            await client.GetAsync(
                ProductsUrl(
                    tenant.Id));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_PasswordOnlyOwner_ReturnsForbidden()
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
            AccessTokenAuthenticationLevel.PasswordOnly);

        var response =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    tenant.Id),
                ValidRequest());

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_MfaOwner_ReturnsCreatedWithDefaultVariant()
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

        var request =
            new CreateProductRequest(
                "iPhone 17 Pro",
                "IPHONE-17-PRO",
                "Flagship phone",
                null,
                999m,
                1099m,
                "usd",
                "iphone-17-pro-256",
                true,
                12,
                3,
                false);

        var response =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    tenant.Id),
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var product =
            await response.Content
                .ReadFromJsonAsync<
                    ProductResponse>();

        Assert.NotNull(
            product);

        Assert.NotEqual(
            Guid.Empty,
            product.ProductId);

        Assert.Equal(
            "iPhone 17 Pro",
            product.Name);

        Assert.Equal(
            "iphone-17-pro",
            product.Slug);

        Assert.Equal(
            999m,
            product.Price);

        Assert.Equal(
            1099m,
            product.CompareAtPrice);

        Assert.Equal(
            "USD",
            product.Currency);

        Assert.Equal(
            "Draft",
            product.Status);

        Assert.False(
            product.IsVisible);

        var variant =
            Assert.Single(
                product.Variants);

        Assert.True(
            variant.IsDefault);

        Assert.Equal(
            "Default",
            variant.Name);

        Assert.Equal(
            "IPHONE-17-PRO-256",
            variant.Sku);

        Assert.True(
            variant.TrackInventory);

        Assert.Equal(
            12,
            variant.Quantity);

        Assert.Equal(
            3,
            variant.LowStockThreshold);

        Assert.True(
            variant.IsAvailableForSale);

        Assert.True(
            variant.IsEnabled);
    }

    [Fact]
    public async Task CreateProduct_DuplicateSlugWithinTenant_ReturnsConflict()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        var first =
            ValidRequest(
                slug:
                    "same-product",
                sku:
                    "SKU-ONE");

        var second =
            ValidRequest(
                slug:
                    "same-product",
                sku:
                    "SKU-TWO");

        var firstResponse =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    setup.Tenant.Id),
                first);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var response =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    setup.Tenant.Id),
                second);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        Assert.Equal(
            "product_slug_already_exists",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task CreateProduct_DuplicateSkuWithinTenant_ReturnsConflict()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        var first =
            ValidRequest(
                slug:
                    "product-one",
                sku:
                    "SHARED-SKU");

        var second =
            ValidRequest(
                slug:
                    "product-two",
                sku:
                    "shared-sku");

        var firstResponse =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    setup.Tenant.Id),
                first);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var response =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    setup.Tenant.Id),
                second);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        Assert.Equal(
            "product_sku_already_exists",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task CreateProduct_SameSlugAndSkuAcrossTenants_IsAllowed()
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

        var request =
            ValidRequest(
                slug:
                    "shared-product",
                sku:
                    "SHARED-SKU");

        var responseA =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    tenantA.Id),
                request);

        var responseB =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    tenantB.Id),
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            responseA.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            responseB.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_CategoryFromAnotherTenant_ReturnsGenericBadRequest()
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

        var categoryB =
            Category.Create(
                tenantB.Id,
                "Phones",
                "phones",
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

        SetAccessToken(
            factory,
            client,
            user,
            AccessTokenAuthenticationLevel.MultiFactor);

        var request =
            new CreateProductRequest(
                "Illegal Product",
                "illegal-product",
                null,
                categoryB.Id.Value,
                100m,
                null,
                "USD",
                "ILLEGAL-SKU",
                true,
                1,
                0,
                false);

        var response =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    tenantA.Id),
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
    public async Task GetProducts_DoesNotLeakProductsFromAnotherTenant()
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

        var responseA =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    tenantA.Id),
                ValidRequest(
                    name:
                        "Product A",
                    slug:
                        "product-a",
                    sku:
                        "SKU-A"));

        Assert.Equal(
            HttpStatusCode.Created,
            responseA.StatusCode);

        var responseB =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    tenantB.Id),
                ValidRequest(
                    name:
                        "Product B",
                    slug:
                        "product-b",
                    sku:
                        "SKU-B"));

        Assert.Equal(
            HttpStatusCode.Created,
            responseB.StatusCode);

        var response =
            await client.GetAsync(
                ProductsUrl(
                    tenantA.Id));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var products =
            await response.Content
                .ReadFromJsonAsync<
                    ProductResponse[]>();

        Assert.NotNull(
            products);

        var product =
            Assert.Single(
                products);

        Assert.Equal(
            "Product A",
            product.Name);

        Assert.Equal(
            "SKU-A",
            Assert.Single(
                product.Variants)
                .Sku);
    }

    [Fact]
    public async Task GetProduct_FromAnotherTenant_ReturnsNotFound()
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

        var createResponse =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    tenantB.Id),
                ValidRequest(
                    name:
                        "Tenant B Product",
                    slug:
                        "tenant-b-product",
                    sku:
                        "TENANT-B-SKU"));

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var product =
            await createResponse.Content
                .ReadFromJsonAsync<
                    ProductResponse>();

        Assert.NotNull(
            product);

        var response =
            await client.GetAsync(
                $"{ProductsUrl(tenantA.Id)}/{product.ProductId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "product_not_found",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task CreateProduct_WithNegativePrice_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        var request =
            new CreateProductRequest(
                "Product",
                "product",
                null,
                null,
                -1m,
                null,
                "USD",
                "SKU-1",
                true,
                1,
                0,
                false);

        var response =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    setup.Tenant.Id),
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
    public async Task CreateProduct_WithInvalidCurrency_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        var request =
            new CreateProductRequest(
                "Product",
                "product",
                null,
                null,
                10m,
                null,
                "US",
                "SKU-1",
                true,
                1,
                0,
                false);

        var response =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    setup.Tenant.Id),
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
    public async Task CreateProduct_WithNegativeInventory_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMfaSetupAsync(
                factory,
                client);

        var request =
            new CreateProductRequest(
                "Product",
                "product",
                null,
                null,
                10m,
                null,
                "USD",
                "SKU-1",
                true,
                -1,
                0,
                false);

        var response =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    setup.Tenant.Id),
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "validation_error",
            await ReadErrorCodeAsync(
                response));
    }

    private static async Task<MfaSetup>
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
                "Products Store",
                "products-store");

        SetAccessToken(
            factory,
            client,
            user,
            AccessTokenAuthenticationLevel.MultiFactor);

        return new MfaSetup(
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
                .ReadFromJsonAsync<
                    RegisterUserResponse>();

        Assert.NotNull(
            registration);

        var userRepository =
            factory.Services
                .GetRequiredService<
                    IUserRepository>();

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
                .GetRequiredService<
                    ITenantRepository>();

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

    private static void SetAccessToken(
        MarketApiFactory factory,
        HttpClient client,
        User user,
        AccessTokenAuthenticationLevel authenticationLevel)
    {
        var accessTokenService =
            factory.Services
                .GetRequiredService<
                    IAccessTokenService>();

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

    private static CreateProductRequest ValidRequest(
        string name = "Test Product",
        string slug = "test-product",
        string sku = "TEST-SKU")
    {
        return new CreateProductRequest(
            name,
            slug,
            "Product description",
            null,
            100m,
            120m,
            "USD",
            sku,
            true,
            10,
            2,
            false);
    }

    private static string ProductsUrl(
        TenantId tenantId)
    {
        return
            $"/api/tenants/{tenantId.Value}/backoffice/products";
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
            .GetProperty(
                "code")
            .GetString();
    }

    private sealed record MfaSetup(
        User User,
        Tenant Tenant);
}