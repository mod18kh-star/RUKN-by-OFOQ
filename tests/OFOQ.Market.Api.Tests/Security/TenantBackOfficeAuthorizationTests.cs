using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Security;

public sealed class TenantBackOfficeAuthorizationTests
{
    private const string Email =
        "backoffice-user@example.com";

    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task BackOffice_WithoutAccessToken_ReturnsUnauthorized()
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

        await tenantRepository.AddAsync(
            tenant);

        var response =
            await client.GetAsync(
                $"/api/tenants/{tenant.Id.Value}/backoffice/context");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task BackOffice_PasswordOnlyToken_WithOwnerMembership_ReturnsForbidden()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                TenantRole.Owner);

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.PasswordOnly);

        var response =
            await GetBackOfficeAsync(
                client,
                setup.Tenant.Id);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Theory]
    [InlineData(TenantRole.Owner)]
    [InlineData(TenantRole.Admin)]
    [InlineData(TenantRole.Manager)]
    [InlineData(TenantRole.Staff)]
    public async Task BackOffice_MfaToken_WithAllowedMembership_ReturnsOk(
        TenantRole role)
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                role);

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        var response =
            await GetBackOfficeAsync(
                client,
                setup.Tenant.Id);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task BackOffice_MfaToken_WithoutMembership_ReturnsForbidden()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                role: null);

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        var response =
            await GetBackOfficeAsync(
                client,
                setup.Tenant.Id);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task BackOffice_MembershipForTenantA_CannotAccessTenantB()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                TenantRole.Owner);

        var secondTenant =
            Tenant.Create(
                "Store Two",
                "store-two",
                DateTimeOffset.UtcNow);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await tenantRepository.AddAsync(
            secondTenant);

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        var response =
            await GetBackOfficeAsync(
                client,
                secondTenant.Id);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task BackOffice_SuspendedUser_ReturnsForbidden()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                TenantRole.Owner);

        /*
         * Issue the token while the account is active,
         * then suspend the user.
         *
         * This proves Back Office authorization re-checks
         * the live user state instead of trusting the JWT.
         */
        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        setup.User.Suspend(
            DateTimeOffset.UtcNow);

        var response =
            await GetBackOfficeAsync(
                client,
                setup.Tenant.Id);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task BackOffice_DeletedMembership_ReturnsForbidden()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                TenantRole.Owner);

        Assert.NotNull(
            setup.Membership);

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        setup.Membership!.Delete(
            DateTimeOffset.UtcNow,
            setup.User.Id.Value);

        var response =
            await GetBackOfficeAsync(
                client,
                setup.Tenant.Id);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task BackOffice_SuspendedTenant_ReturnsForbidden()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                TenantRole.Owner);

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        setup.Tenant.Suspend(
            DateTimeOffset.UtcNow,
            setup.User.Id.Value);

        var response =
            await GetBackOfficeAsync(
                client,
                setup.Tenant.Id);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task BackOffice_ActiveTenant_WithMfaOwner_ReturnsOk()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                TenantRole.Owner);

        setup.Tenant.Activate(
            DateTimeOffset.UtcNow,
            setup.User.Id.Value);

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        var response =
            await GetBackOfficeAsync(
                client,
                setup.Tenant.Id);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    private static async Task<BackOfficeSetup>
        CreateSetupAsync(
            MarketApiFactory factory,
            TenantRole? role)
    {
        using var client =
            factory.CreateClient();

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

        var userId =
            UserId.From(
                registration.UserId);

        var userRepository =
            factory.Services
                .GetRequiredService<
                    IUserRepository>();

        var user =
            await userRepository
                .GetByIdAsync(
                    userId);

        Assert.NotNull(
            user);

        var tenant =
            Tenant.Create(
                "Back Office Store",
                "back-office-store",
                DateTimeOffset.UtcNow,
                user.Id.Value);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await tenantRepository.AddAsync(
            tenant);

        TenantMembership? membership =
            null;

        if (role.HasValue)
        {
            membership =
                TenantMembership.Create(
                    tenant.Id,
                    user.Id,
                    role.Value,
                    DateTimeOffset.UtcNow,
                    user.Id.Value);

            var membershipRepository =
                factory.Services
                    .GetRequiredService<
                        ITenantMembershipRepository>();

            await membershipRepository.AddAsync(
                membership);
        }

        return new BackOfficeSetup(
            user,
            tenant,
            membership);
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

    private static Task<HttpResponseMessage>
        GetBackOfficeAsync(
            HttpClient client,
            TenantId tenantId)
    {
        return client.GetAsync(
            $"/api/tenants/{tenantId.Value}/backoffice/context");
    }

    private sealed record BackOfficeSetup(
        User User,
        Tenant Tenant,
        TenantMembership? Membership);
}