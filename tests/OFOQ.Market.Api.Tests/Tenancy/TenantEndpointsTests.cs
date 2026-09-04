using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Contracts.Tenancy;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Tenancy;

public sealed class TenantEndpointsTests
{
    [Fact]
    public async Task PostTenant_WithoutAccessToken_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/tenants",
                new CreateTenantRequest(
                    "Turks Store",
                    "turks"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task PostTenant_WithPasswordOnlyToken_CreatesTenantAndOwnerMembership()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var user =
            await AuthenticateUserAsync(
                factory,
                client,
                "owner@example.com");

        var response =
            await client.PostAsJsonAsync(
                "/api/tenants",
                new CreateTenantRequest(
                    "Turks Store",
                    "TURKS"));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    CreateTenantResponse>();

        Assert.NotNull(
            result);

        Assert.NotEqual(
            Guid.Empty,
            result.TenantId);

        Assert.Equal(
            "Turks Store",
            result.Name);

        Assert.Equal(
            "turks",
            result.Slug);

        Assert.Equal(
            "Draft",
            result.Status);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        var tenant =
            await tenantRepository.GetByIdAsync(
                TenantId.From(
                    result.TenantId));

        Assert.NotNull(
            tenant);

        Assert.Equal(
            user.Id.Value,
            tenant.CreatedByUserId);

        var membershipRepository =
            factory.Services
                .GetRequiredService<
                    InMemoryTenantMembershipRepository>();

        var membership =
            Assert.Single(
                membershipRepository.Items);

        Assert.Equal(
            result.TenantId,
            membership.TenantId.Value);

        Assert.Equal(
            user.Id,
            membership.UserId);

        Assert.Equal(
            TenantRole.Owner,
            membership.Role);

        Assert.Equal(
            user.Id.Value,
            membership.CreatedByUserId);
    }

    [Fact]
    public async Task PostTenant_ReturnsConflict_WhenSlugAlreadyExists()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        await AuthenticateUserAsync(
            factory,
            client,
            "owner@example.com");

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/tenants",
                new CreateTenantRequest(
                    "First Store",
                    "turks"));

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var response =
            await client.PostAsJsonAsync(
                "/api/tenants",
                new CreateTenantRequest(
                    "Second Store",
                    "turks"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        Assert.Equal(
            "tenant_slug_already_exists",
            json.RootElement
                .GetProperty(
                    "code")
                .GetString());
    }

    [Fact]
    public async Task PostTenant_WhenLiveUserIsSuspended_ReturnsForbidden()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var user =
            await AuthenticateUserAsync(
                factory,
                client,
                "owner@example.com");

        /*
         * Token was issued while Active.
         * The live account is suspended afterwards.
         */
        user.Suspend(
            DateTimeOffset.UtcNow);

        var response =
            await client.PostAsJsonAsync(
                "/api/tenants",
                new CreateTenantRequest(
                    "Blocked Store",
                    "blocked-store"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        var tenant =
            await tenantRepository
                .GetBySlugAsync(
                    TenantSlug.Create(
                        "blocked-store"));

        Assert.Null(
            tenant);
    }

    [Fact]
    public async Task GetTenant_ReturnsOk_WhenTenantExists()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        await AuthenticateUserAsync(
            factory,
            client,
            "owner@example.com");

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/tenants",
                new CreateTenantRequest(
                    "Turks Store",
                    "turks"));

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<
                    CreateTenantResponse>();

        Assert.NotNull(
            created);

        var response =
            await client.GetAsync(
                $"/api/tenants/{created.TenantId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    GetTenantByIdResponse>();

        Assert.NotNull(
            result);

        Assert.Equal(
            created.TenantId,
            result.TenantId);

        Assert.Equal(
            "Turks Store",
            result.Name);

        Assert.Equal(
            "turks",
            result.Slug);

        Assert.Equal(
            "Draft",
            result.Status);
    }

    [Fact]
    public async Task GetTenant_ReturnsNotFound_WhenTenantDoesNotExist()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync(
                $"/api/tenants/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetTenant_ReturnsBadRequest_WhenTenantIdIsInvalid()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync(
                "/api/tenants/not-a-guid");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private static async Task<User> AuthenticateUserAsync(
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

        /*
         * Password-only assurance is intentionally sufficient
         * for store creation/onboarding.
         *
         * Back Office remains MFA-only.
         */
        var accessTokenService =
            factory.Services
                .GetRequiredService<
                    IAccessTokenService>();

        var accessToken =
            accessTokenService.Create(
                user.Id,
                user.Email.Value,
                DateTimeOffset.UtcNow,
                AccessTokenAuthenticationLevel.PasswordOnly);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken.Token);

        return user;
    }
}