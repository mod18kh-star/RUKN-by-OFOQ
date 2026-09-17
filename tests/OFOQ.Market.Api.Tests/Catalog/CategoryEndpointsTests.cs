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
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Catalog;

public sealed class CategoryEndpointsTests
{
    [Fact]
    public async Task CreateCategory_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenantId =
            Guid.NewGuid();

        var response =
            await client.PostAsJsonAsync(
                $"/api/tenants/{tenantId}/backoffice/categories/",
                new CreateCategoryRequest(
                    "Phones",
                    "phones"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithPasswordOnlyToken_ReturnsForbidden()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMerchantAsync(
                factory,
                client,
                "merchant@example.com",
                AccessTokenAuthenticationLevel.PasswordOnly);

        var response =
            await client.PostAsJsonAsync(
                $"/api/tenants/{setup.Tenant.Id.Value}/backoffice/categories/",
                new CreateCategoryRequest(
                    "Phones",
                    "phones"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithMfaOwner_ReturnsCreated()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMerchantAsync(
                factory,
                client,
                "merchant@example.com",
                AccessTokenAuthenticationLevel.MultiFactor);

        var response =
            await client.PostAsJsonAsync(
                $"/api/tenants/{setup.Tenant.Id.Value}/backoffice/categories/",
                new CreateCategoryRequest(
                    "Phones",
                    "PHONES",
                    ParentCategoryId: null,
                    SortOrder: 10));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    CategoryResponse>();

        Assert.NotNull(
            result);

        Assert.NotEqual(
            Guid.Empty,
            result.CategoryId);

        Assert.Equal(
            "Phones",
            result.Name);

        Assert.Equal(
            "phones",
            result.Slug);

        Assert.Null(
            result.ParentCategoryId);

        Assert.Equal(
            10,
            result.SortOrder);

        Assert.True(
            result.IsVisible);
    }

    [Fact]
    public async Task CreateCategory_DuplicateSlugInSameTenant_ReturnsConflict()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMerchantAsync(
                factory,
                client,
                "merchant@example.com",
                AccessTokenAuthenticationLevel.MultiFactor);

        var url =
            $"/api/tenants/{setup.Tenant.Id.Value}/backoffice/categories/";

        var first =
            await client.PostAsJsonAsync(
                url,
                new CreateCategoryRequest(
                    "Phones",
                    "phones"));

        Assert.Equal(
            HttpStatusCode.Created,
            first.StatusCode);

        var second =
            await client.PostAsJsonAsync(
                url,
                new CreateCategoryRequest(
                    "Other Phones",
                    "PHONES"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            second.StatusCode);

        using var json =
            JsonDocument.Parse(
                await second.Content
                    .ReadAsStringAsync());

        Assert.Equal(
            "category_slug_already_exists",
            json.RootElement
                .GetProperty(
                    "code")
                .GetString());
    }

    [Fact]
    public async Task SameSlug_IsAllowedAcrossDifferentTenants()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateUserAsync(
                factory,
                client,
                "merchant@example.com");

        var tenantA =
            await AddTenantAsync(
                factory,
                setup.User,
                "Store A",
                "store-a");

        var tenantB =
            await AddTenantAsync(
                factory,
                setup.User,
                "Store B",
                "store-b");

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        var responseA =
            await client.PostAsJsonAsync(
                $"/api/tenants/{tenantA.Id.Value}/backoffice/categories/",
                new CreateCategoryRequest(
                    "Phones A",
                    "phones"));

        Assert.Equal(
            HttpStatusCode.Created,
            responseA.StatusCode);

        var responseB =
            await client.PostAsJsonAsync(
                $"/api/tenants/{tenantB.Id.Value}/backoffice/categories/",
                new CreateCategoryRequest(
                    "Phones B",
                    "phones"));

        Assert.Equal(
            HttpStatusCode.Created,
            responseB.StatusCode);
    }

    [Fact]
    public async Task ListCategories_DoesNotLeakOtherTenantCategories()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateUserAsync(
                factory,
                client,
                "merchant@example.com");

        var tenantA =
            await AddTenantAsync(
                factory,
                setup.User,
                "Store A",
                "store-a");

        var tenantB =
            await AddTenantAsync(
                factory,
                setup.User,
                "Store B",
                "store-b");

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        await AssertCreatedAsync(
            client.PostAsJsonAsync(
                $"/api/tenants/{tenantA.Id.Value}/backoffice/categories/",
                new CreateCategoryRequest(
                    "Tenant A Category",
                    "tenant-a-category")));

        await AssertCreatedAsync(
            client.PostAsJsonAsync(
                $"/api/tenants/{tenantB.Id.Value}/backoffice/categories/",
                new CreateCategoryRequest(
                    "Tenant B Category",
                    "tenant-b-category")));

        var response =
            await client.GetAsync(
                $"/api/tenants/{tenantA.Id.Value}/backoffice/categories/");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var categories =
            await response.Content
                .ReadFromJsonAsync<
                    CategoryResponse[]>();

        Assert.NotNull(
            categories);

        var category =
            Assert.Single(
                categories);

        Assert.Equal(
            "Tenant A Category",
            category.Name);

        Assert.Equal(
            "tenant-a-category",
            category.Slug);
    }

    [Fact]
    public async Task GetCategory_FromAnotherTenant_ReturnsNotFound()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateUserAsync(
                factory,
                client,
                "merchant@example.com");

        var tenantA =
            await AddTenantAsync(
                factory,
                setup.User,
                "Store A",
                "store-a");

        var tenantB =
            await AddTenantAsync(
                factory,
                setup.User,
                "Store B",
                "store-b");

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        var createResponse =
            await client.PostAsJsonAsync(
                $"/api/tenants/{tenantB.Id.Value}/backoffice/categories/",
                new CreateCategoryRequest(
                    "Private B",
                    "private-b"));

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var categoryB =
            await createResponse.Content
                .ReadFromJsonAsync<
                    CategoryResponse>();

        Assert.NotNull(
            categoryB);

        var response =
            await client.GetAsync(
                $"/api/tenants/{tenantA.Id.Value}/backoffice/categories/{categoryB.CategoryId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithParentFromAnotherTenant_ReturnsParentNotFound()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateUserAsync(
                factory,
                client,
                "merchant@example.com");

        var tenantA =
            await AddTenantAsync(
                factory,
                setup.User,
                "Store A",
                "store-a");

        var tenantB =
            await AddTenantAsync(
                factory,
                setup.User,
                "Store B",
                "store-b");

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        var parentResponse =
            await client.PostAsJsonAsync(
                $"/api/tenants/{tenantB.Id.Value}/backoffice/categories/",
                new CreateCategoryRequest(
                    "Parent B",
                    "parent-b"));

        Assert.Equal(
            HttpStatusCode.Created,
            parentResponse.StatusCode);

        var parent =
            await parentResponse.Content
                .ReadFromJsonAsync<
                    CategoryResponse>();

        Assert.NotNull(
            parent);

        var response =
            await client.PostAsJsonAsync(
                $"/api/tenants/{tenantA.Id.Value}/backoffice/categories/",
                new CreateCategoryRequest(
                    "Child A",
                    "child-a",
                    parent.CategoryId));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        Assert.Equal(
            "category_parent_not_found",
            json.RootElement
                .GetProperty(
                    "code")
                .GetString());
    }


    [Fact]
    public async Task CreateCategory_WithoutPosition_AppendsToSiblingGroup()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMerchantAsync(
                factory,
                client,
                "merchant@example.com",
                AccessTokenAuthenticationLevel.MultiFactor);

        var url =
            $"/api/tenants/{setup.Tenant.Id.Value}/backoffice/categories/";

        var firstResponse =
            await client.PostAsJsonAsync(
                url,
                new CreateCategoryRequest(
                    "First",
                    "first"));

        var secondResponse =
            await client.PostAsJsonAsync(
                url,
                new CreateCategoryRequest(
                    "Second",
                    "second"));

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            secondResponse.StatusCode);

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<CategoryResponse>();

        var second =
            await secondResponse.Content
                .ReadFromJsonAsync<CategoryResponse>();

        Assert.NotNull(
            first);

        Assert.NotNull(
            second);

        Assert.Equal(
            1,
            first.SortOrder);

        Assert.Equal(
            2,
            second.SortOrder);
    }

    [Fact]
    public async Task CreateCategory_AtPositionOne_ShiftsExistingSibling()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMerchantAsync(
                factory,
                client,
                "merchant@example.com",
                AccessTokenAuthenticationLevel.MultiFactor);

        var url =
            $"/api/tenants/{setup.Tenant.Id.Value}/backoffice/categories/";

        await AssertCreatedAsync(
            client.PostAsJsonAsync(
                url,
                new CreateCategoryRequest(
                    "Existing",
                    "existing")));

        var insertedResponse =
            await client.PostAsJsonAsync(
                url,
                new CreateCategoryRequest(
                    "Inserted",
                    "inserted",
                    Position: 1));

        Assert.Equal(
            HttpStatusCode.Created,
            insertedResponse.StatusCode);

        var listResponse =
            await client.GetAsync(
                url);

        var categories =
            await listResponse.Content
                .ReadFromJsonAsync<CategoryResponse[]>();

        Assert.NotNull(
            categories);

        var inserted =
            Assert.Single(
                categories.Where(
                    item =>
                        item.Slug ==
                        "inserted"));

        var existing =
            Assert.Single(
                categories.Where(
                    item =>
                        item.Slug ==
                        "existing"));

        Assert.Equal(
            1,
            inserted.SortOrder);

        Assert.Equal(
            2,
            existing.SortOrder);
    }

    [Fact]
    public async Task MoveCategory_BeneathDescendant_ReturnsHierarchyCycle()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMerchantAsync(
                factory,
                client,
                "merchant@example.com",
                AccessTokenAuthenticationLevel.MultiFactor);

        var url =
            $"/api/tenants/{setup.Tenant.Id.Value}/backoffice/categories/";

        var rootResponse =
            await client.PostAsJsonAsync(
                url,
                new CreateCategoryRequest(
                    "Root",
                    "root"));

        var root =
            await rootResponse.Content
                .ReadFromJsonAsync<CategoryResponse>();

        Assert.NotNull(
            root);

        var childResponse =
            await client.PostAsJsonAsync(
                url,
                new CreateCategoryRequest(
                    "Child",
                    "child",
                    ParentCategoryId:
                        root.CategoryId));

        var child =
            await childResponse.Content
                .ReadFromJsonAsync<CategoryResponse>();

        Assert.NotNull(
            child);

        var moveResponse =
            await client.PutAsJsonAsync(
                $"{url}{root.CategoryId}/placement",
                new MoveCategoryRequest(
                    ParentCategoryId:
                        child.CategoryId,
                    Position: 1));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            moveResponse.StatusCode);

        using var json =
            JsonDocument.Parse(
                await moveResponse.Content
                    .ReadAsStringAsync());

        Assert.Equal(
            "category_hierarchy_cycle",
            json.RootElement
                .GetProperty(
                    "code")
                .GetString());
    }

    [Fact]
    public async Task MoveCategory_ToFirstPosition_ReordersSiblingGroup()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMerchantAsync(
                factory,
                client,
                "merchant@example.com",
                AccessTokenAuthenticationLevel.MultiFactor);

        var url =
            $"/api/tenants/{setup.Tenant.Id.Value}/backoffice/categories/";

        var firstResponse =
            await client.PostAsJsonAsync(
                url,
                new CreateCategoryRequest(
                    "First",
                    "first"));

        var secondResponse =
            await client.PostAsJsonAsync(
                url,
                new CreateCategoryRequest(
                    "Second",
                    "second"));

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<CategoryResponse>();

        var second =
            await secondResponse.Content
                .ReadFromJsonAsync<CategoryResponse>();

        Assert.NotNull(
            first);

        Assert.NotNull(
            second);

        var moveResponse =
            await client.PutAsJsonAsync(
                $"{url}{second.CategoryId}/placement",
                new MoveCategoryRequest(
                    Position: 1));

        Assert.Equal(
            HttpStatusCode.OK,
            moveResponse.StatusCode);

        var listResponse =
            await client.GetAsync(
                url);

        var categories =
            await listResponse.Content
                .ReadFromJsonAsync<CategoryResponse[]>();

        Assert.NotNull(
            categories);

        var moved =
            Assert.Single(
                categories.Where(
                    item =>
                        item.CategoryId ==
                        second.CategoryId));

        var shifted =
            Assert.Single(
                categories.Where(
                    item =>
                        item.CategoryId ==
                        first.CategoryId));

        Assert.Equal(
            1,
            moved.SortOrder);

        Assert.Equal(
            2,
            shifted.SortOrder);
    }

    private static async Task<MerchantSetup> CreateMerchantAsync(
        MarketApiFactory factory,
        HttpClient client,
        string email,
        AccessTokenAuthenticationLevel authenticationLevel)
    {
        var userSetup =
            await CreateUserAsync(
                factory,
                client,
                email);

        var tenant =
            await AddTenantAsync(
                factory,
                userSetup.User,
                "Merchant Store",
                "merchant-store");

        SetAccessToken(
            factory,
            client,
            userSetup.User,
            authenticationLevel);

        return new MerchantSetup(
            userSetup.User,
            tenant);
    }

    private static async Task<UserSetup> CreateUserAsync(
        MarketApiFactory factory,
        HttpClient client,
        string email)
    {
        const string password =
            "StrongPassword123";

        var registrationResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    email,
                    password));

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

        return new UserSetup(
            user);
    }

    private static async Task<Tenant> AddTenantAsync(
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

        var membershipRepository =
            factory.Services
                .GetRequiredService<
                    ITenantMembershipRepository>();

        await membershipRepository
            .AddAsync(
                TenantMembership.Create(
                    tenant.Id,
                    user.Id,
                    TenantRole.Owner,
                    now,
                    user.Id.Value));

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

        var token =
            accessTokenService.Create(
                user.Id,
                user.Email.Value,
                DateTimeOffset.UtcNow,
                authenticationLevel);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token.Token);
    }

    private static async Task AssertCreatedAsync(
        Task<HttpResponseMessage> responseTask)
    {
        var response =
            await responseTask;

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);
    }

    private sealed record UserSetup(
        User User);

    private sealed record MerchantSetup(
        User User,
        Tenant Tenant);
}