using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Commerce.Configuration;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Commerce;

public sealed class CommerceConfigurationApiTests
{
    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task Owner_CanConfigurePrimaryApparelVertical()
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

        var response =
            await client.PutAsJsonAsync(
                VerticalsUrl(
                    setup.Tenant),
                new ConfigureCommerceVerticalRequest(
                    "Apparel",
                    true,
                    true));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var vertical =
            await response.Content
                .ReadFromJsonAsync<
                    CommerceVerticalResponse>();

        Assert.NotNull(
            vertical);

        Assert.Equal(
            "Apparel",
            vertical.VerticalType);

        Assert.Equal(
            "apparel",
            vertical.Code);

        Assert.True(
            vertical.Enabled);

        Assert.True(
            vertical.Primary);

        var profileResponse =
            await client.GetAsync(
                ProfileUrl(
                    setup.Tenant));

        Assert.Equal(
            HttpStatusCode.OK,
            profileResponse.StatusCode);

        var profile =
            await profileResponse.Content
                .ReadFromJsonAsync<
                    CommerceProfileResponse>();

        Assert.NotNull(
            profile);

        var configuredVertical =
            Assert.Single(
                profile.Verticals);

        Assert.True(
            configuredVertical.Primary);

        Assert.Contains(
            profile.Capabilities,
            capability =>
                capability.CapabilityType ==
                    "PhysicalStock" &&
                capability.Enabled);

        Assert.Contains(
            profile.Capabilities,
            capability =>
                capability.CapabilityType ==
                    "Variants" &&
                capability.Enabled);
    }

    [Fact]
    public async Task Admin_CanCombineMultipleVerticals()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                TenantRole.Admin);

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        var apparelResponse =
            await client.PutAsJsonAsync(
                VerticalsUrl(
                    setup.Tenant),
                new ConfigureCommerceVerticalRequest(
                    "Apparel",
                    true,
                    true));

        Assert.Equal(
            HttpStatusCode.OK,
            apparelResponse.StatusCode);

        var perfumeResponse =
            await client.PutAsJsonAsync(
                VerticalsUrl(
                    setup.Tenant),
                new ConfigureCommerceVerticalRequest(
                    "Perfumes",
                    true,
                    false));

        Assert.Equal(
            HttpStatusCode.OK,
            perfumeResponse.StatusCode);

        var profileResponse =
            await client.GetAsync(
                ProfileUrl(
                    setup.Tenant));

        Assert.Equal(
            HttpStatusCode.OK,
            profileResponse.StatusCode);

        var profile =
            await profileResponse.Content
                .ReadFromJsonAsync<
                    CommerceProfileResponse>();

        Assert.NotNull(
            profile);

        Assert.Equal(
            2,
            profile.Verticals.Count);

        Assert.Single(
            profile.Verticals,
            vertical =>
                vertical.Primary);

        Assert.Contains(
            profile.Capabilities,
            capability =>
                capability.CapabilityType ==
                    "Bundles" &&
                capability.Enabled);
    }

    [Fact]
    public async Task CapabilityOverride_CanDisableAndRestoreDefaultCapability()
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

        var verticalResponse =
            await client.PutAsJsonAsync(
                VerticalsUrl(
                    setup.Tenant),
                new ConfigureCommerceVerticalRequest(
                    "Apparel",
                    true,
                    true));

        Assert.Equal(
            HttpStatusCode.OK,
            verticalResponse.StatusCode);

        var disableResponse =
            await client.PutAsJsonAsync(
                CapabilitiesUrl(
                    setup.Tenant),
                new SetCommerceCapabilityOverrideRequest(
                    "MultiWarehouse",
                    false));

        Assert.Equal(
            HttpStatusCode.OK,
            disableResponse.StatusCode);

        var disabledProfile =
            await disableResponse.Content
                .ReadFromJsonAsync<
                    CommerceProfileResponse>();

        Assert.NotNull(
            disabledProfile);

        var disabledCapability =
            Assert.Single(
                disabledProfile.Capabilities,
                capability =>
                    capability.CapabilityType ==
                    "MultiWarehouse");

        Assert.False(
            disabledCapability.Enabled);

        Assert.True(
            disabledCapability.Overridden);

        var restoreResponse =
            await client.PutAsJsonAsync(
                CapabilitiesUrl(
                    setup.Tenant),
                new SetCommerceCapabilityOverrideRequest(
                    "MultiWarehouse",
                    null));

        Assert.Equal(
            HttpStatusCode.OK,
            restoreResponse.StatusCode);

        var restoredProfile =
            await restoreResponse.Content
                .ReadFromJsonAsync<
                    CommerceProfileResponse>();

        Assert.NotNull(
            restoredProfile);

        var restoredCapability =
            Assert.Single(
                restoredProfile.Capabilities,
                capability =>
                    capability.CapabilityType ==
                    "MultiWarehouse");

        Assert.True(
            restoredCapability.Enabled);

        Assert.False(
            restoredCapability.Overridden);
    }

    [Fact]
    public async Task FirstVertical_MustBePrimary()
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

        var response =
            await client.PutAsJsonAsync(
                VerticalsUrl(
                    setup.Tenant),
                new ConfigureCommerceVerticalRequest(
                    "Apparel",
                    true,
                    false));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Theory]
    [InlineData(TenantRole.Manager)]
    [InlineData(TenantRole.Staff)]
    public async Task ManagerAndStaff_CannotManageCommerceConfiguration(
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
            await client.GetAsync(
                ProfileUrl(
                    setup.Tenant));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task PasswordOnlyOwner_CannotManageCommerceConfiguration()
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
            await client.GetAsync(
                ProfileUrl(
                    setup.Tenant));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task UserWithoutMembership_CannotManageCommerceConfiguration()
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
            await client.GetAsync(
                ProfileUrl(
                    setup.Tenant));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CommerceConfiguration_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            Tenant.Create(
                "Anonymous Commerce Store",
                $"anonymous-commerce-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow);

        var tenantRepository =
            factory.Services.GetRequiredService<
                ITenantRepository>();

        await tenantRepository.AddAsync(
            tenant);

        client.DefaultRequestHeaders.Authorization =
            null;

        var response =
            await client.GetAsync(
                ProfileUrl(
                    tenant));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CommerceConfiguration_IsIsolatedBetweenTenants()
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
                "Second Commerce Store",
                $"second-commerce-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow,
                setup.User.Id.Value);

        var tenantRepository =
            factory.Services.GetRequiredService<
                ITenantRepository>();

        await tenantRepository.AddAsync(
            secondTenant);

        var secondMembership =
            TenantMembership.Create(
                secondTenant.Id,
                setup.User.Id,
                TenantRole.Owner,
                DateTimeOffset.UtcNow,
                setup.User.Id.Value);

        var membershipRepository =
            factory.Services.GetRequiredService<
                ITenantMembershipRepository>();

        await membershipRepository.AddAsync(
            secondMembership);

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        var configureResponse =
            await client.PutAsJsonAsync(
                VerticalsUrl(
                    setup.Tenant),
                new ConfigureCommerceVerticalRequest(
                    "RealEstate",
                    true,
                    true));

        Assert.Equal(
            HttpStatusCode.OK,
            configureResponse.StatusCode);

        var firstProfileResponse =
            await client.GetAsync(
                ProfileUrl(
                    setup.Tenant));

        Assert.Equal(
            HttpStatusCode.OK,
            firstProfileResponse.StatusCode);

        var firstProfile =
            await firstProfileResponse.Content
                .ReadFromJsonAsync<
                    CommerceProfileResponse>();

        Assert.NotNull(
            firstProfile);

        Assert.Single(
            firstProfile.Verticals);

        Assert.Contains(
            firstProfile.Capabilities,
            capability =>
                capability.CapabilityType ==
                    "Listings" &&
                capability.Enabled);

        var secondProfileResponse =
            await client.GetAsync(
                ProfileUrl(
                    secondTenant));

        Assert.Equal(
            HttpStatusCode.OK,
            secondProfileResponse.StatusCode);

        var secondProfile =
            await secondProfileResponse.Content
                .ReadFromJsonAsync<
                    CommerceProfileResponse>();

        Assert.NotNull(
            secondProfile);

        Assert.Empty(
            secondProfile.Verticals);

        Assert.DoesNotContain(
            secondProfile.Capabilities,
            capability =>
                capability.Enabled);
    }

    private static async Task<CommerceSetup> CreateSetupAsync(
        MarketApiFactory factory,
        TenantRole? role)
    {
        using var client =
            factory.CreateClient();

        var email =
            $"commerce-admin-{Guid.NewGuid():N}@example.com";

        var registrationResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    email,
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
            factory.Services.GetRequiredService<
                IUserRepository>();

        var user =
            await userRepository.GetByIdAsync(
                userId);

        Assert.NotNull(
            user);

        var tenant =
            Tenant.Create(
                "Commerce Configuration Store",
                $"commerce-config-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow,
                user.Id.Value);

        var tenantRepository =
            factory.Services.GetRequiredService<
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
                factory.Services.GetRequiredService<
                    ITenantMembershipRepository>();

            await membershipRepository.AddAsync(
                membership);
        }

        return new CommerceSetup(
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
            factory.Services.GetRequiredService<
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

    private static string ProfileUrl(
        Tenant tenant)
    {
        return
            $"/api/tenants/{tenant.Id.Value}/backoffice/commerce/profile";
    }

    private static string VerticalsUrl(
        Tenant tenant)
    {
        return
            $"/api/tenants/{tenant.Id.Value}/backoffice/commerce/verticals";
    }

    private static string CapabilitiesUrl(
        Tenant tenant)
    {
        return
            $"/api/tenants/{tenant.Id.Value}/backoffice/commerce/capabilities";
    }

    private sealed record CommerceSetup(
        User User,
        Tenant Tenant,
        TenantMembership? Membership);
}