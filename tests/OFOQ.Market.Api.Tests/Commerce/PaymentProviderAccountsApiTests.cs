using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Commerce.Payments;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Commerce;

public sealed class PaymentProviderAccountsApiTests
{
    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task ProviderAccount_CreateCredentialsAndList_DoesNotExposeSecret()
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

        var account =
            await CreateProviderAccountAsync(
                client,
                setup.Tenant,
                "provider-a",
                "Provider A",
                "Sandbox");

        Assert.False(
            account.IsEnabled);

        Assert.False(
            account.HasCredentials);

        Assert.Equal(
            0,
            account.CredentialsVersion);

        const string secret =
            "provider-secret-value-that-must-never-leak";

        var credentialsResponse =
            await client.PutAsJsonAsync(
                ProviderAccountUrl(
                    setup.Tenant,
                    account.AccountId,
                    "credentials"),
                new UpdatePaymentProviderCredentialsRequest(
                    new Dictionary<string, string>
                    {
                        ["api_key"] =
                            secret,

                        ["merchant_id"] =
                            "merchant-123"
                    }));

        Assert.Equal(
            HttpStatusCode.OK,
            credentialsResponse.StatusCode);

        var credentialsBody =
            await credentialsResponse.Content
                .ReadAsStringAsync();

        Assert.DoesNotContain(
            secret,
            credentialsBody,
            StringComparison.Ordinal);

        var updated =
            await credentialsResponse.Content
                .ReadFromJsonAsync<
                    PaymentProviderAccountResponse>();

        Assert.NotNull(
            updated);

        Assert.True(
            updated.HasCredentials);

        Assert.Equal(
            1,
            updated.CredentialsVersion);

        var store =
            factory.Services.GetRequiredService<
                InMemoryTenantPaymentProviderAccountStore>();

        lock (store.SyncRoot)
        {
            var stored =
                Assert.Single(
                    store.Items,
                    item =>
                        item.Id.Value ==
                        account.AccountId);

            Assert.True(
                stored.HasCredentials);

            Assert.NotNull(
                stored.ProtectedCredentials);

            Assert.DoesNotContain(
                secret,
                stored.ProtectedCredentials!,
                StringComparison.Ordinal);
        }

        var listResponse =
            await client.GetAsync(
                ProviderAccountsUrl(
                    setup.Tenant));

        Assert.Equal(
            HttpStatusCode.OK,
            listResponse.StatusCode);

        var listBody =
            await listResponse.Content
                .ReadAsStringAsync();

        Assert.DoesNotContain(
            secret,
            listBody,
            StringComparison.Ordinal);

        var accounts =
            await listResponse.Content
                .ReadFromJsonAsync<
                    PaymentProviderAccountResponse[]>();

        Assert.NotNull(
            accounts);

        var listed =
            Assert.Single(
                accounts);

        Assert.Equal(
            account.AccountId,
            listed.AccountId);

        Assert.True(
            listed.HasCredentials);

        Assert.Equal(
            1,
            listed.CredentialsVersion);
    }

    [Fact]
    public async Task WalletCapabilities_ApplePayAndSamsungPay_CanBeEnabled()
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

        var account =
            await CreateProviderAccountAsync(
                client,
                setup.Tenant,
                "provider-wallet",
                "Wallet Provider",
                "Production");

        var appleResponse =
            await client.PutAsJsonAsync(
                ProviderAccountUrl(
                    setup.Tenant,
                    account.AccountId,
                    "wallets"),
                new SetPaymentWalletCapabilityRequest(
                    "ApplePay",
                    true));

        Assert.Equal(
            HttpStatusCode.OK,
            appleResponse.StatusCode);

        var apple =
            await appleResponse.Content
                .ReadFromJsonAsync<
                    PaymentWalletCapabilityResponse>();

        Assert.NotNull(
            apple);

        Assert.Equal(
            "ApplePay",
            apple.WalletType);

        Assert.True(
            apple.IsEnabled);

        var samsungResponse =
            await client.PutAsJsonAsync(
                ProviderAccountUrl(
                    setup.Tenant,
                    account.AccountId,
                    "wallets"),
                new SetPaymentWalletCapabilityRequest(
                    "SamsungPay",
                    true));

        Assert.Equal(
            HttpStatusCode.OK,
            samsungResponse.StatusCode);

        var samsung =
            await samsungResponse.Content
                .ReadFromJsonAsync<
                    PaymentWalletCapabilityResponse>();

        Assert.NotNull(
            samsung);

        Assert.Equal(
            "SamsungPay",
            samsung.WalletType);

        Assert.True(
            samsung.IsEnabled);

        var listResponse =
            await client.GetAsync(
                ProviderAccountsUrl(
                    setup.Tenant));

        Assert.Equal(
            HttpStatusCode.OK,
            listResponse.StatusCode);

        var accounts =
            await listResponse.Content
                .ReadFromJsonAsync<
                    PaymentProviderAccountResponse[]>();

        Assert.NotNull(
            accounts);

        var listed =
            Assert.Single(
                accounts);

        Assert.Equal(
            2,
            listed.Wallets.Count);

        Assert.Contains(
            listed.Wallets,
            wallet =>
                wallet.WalletType ==
                    "ApplePay" &&
                wallet.IsEnabled);

        Assert.Contains(
            listed.Wallets,
            wallet =>
                wallet.WalletType ==
                    "SamsungPay" &&
                wallet.IsEnabled);
    }

    [Fact]
    public async Task ProviderAccount_CrossTenantAccess_IsHidden()
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
                "Tenant B Provider Store",
                $"tenant-b-provider-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow,
                setup.User.Id.Value);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
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
            factory.Services
                .GetRequiredService<
                    ITenantMembershipRepository>();

        await membershipRepository.AddAsync(
            secondMembership);

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.MultiFactor);

        var accountA =
            await CreateProviderAccountAsync(
                client,
                setup.Tenant,
                "provider-isolation",
                "Isolation Provider",
                "Sandbox");

        var tenantBListResponse =
            await client.GetAsync(
                ProviderAccountsUrl(
                    secondTenant));

        Assert.Equal(
            HttpStatusCode.OK,
            tenantBListResponse.StatusCode);

        var tenantBAccounts =
            await tenantBListResponse.Content
                .ReadFromJsonAsync<
                    PaymentProviderAccountResponse[]>();

        Assert.NotNull(
            tenantBAccounts);

        Assert.Empty(
            tenantBAccounts);

        var crossTenantCredentialsResponse =
            await client.PutAsJsonAsync(
                ProviderAccountUrl(
                    secondTenant,
                    accountA.AccountId,
                    "credentials"),
                new UpdatePaymentProviderCredentialsRequest(
                    new Dictionary<string, string>
                    {
                        ["api_key"] =
                            "cross-tenant-secret"
                    }));

        Assert.Equal(
            HttpStatusCode.NotFound,
            crossTenantCredentialsResponse.StatusCode);
    }

    [Fact]
    public async Task ProviderAccount_CannotBeEnabledBeforeCredentialsAreConfigured()
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

        var account =
            await CreateProviderAccountAsync(
                client,
                setup.Tenant,
                "provider-state",
                "State Provider",
                "Production");

        var response =
            await client.PutAsJsonAsync(
                ProviderAccountUrl(
                    setup.Tenant,
                    account.AccountId,
                    "state"),
                new SetPaymentProviderAccountStateRequest(
                    true));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var store =
            factory.Services.GetRequiredService<
                InMemoryTenantPaymentProviderAccountStore>();

        lock (store.SyncRoot)
        {
            var stored =
                Assert.Single(
                    store.Items,
                    item =>
                        item.Id.Value ==
                        account.AccountId);

            Assert.False(
                stored.IsEnabled);
        }
    }

    [Fact]
    public async Task ProviderAccount_OnlyOneAccountForProviderCanBeEnabled()
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

        var sandboxAccount =
            await CreateProviderAccountAsync(
                client,
                setup.Tenant,
                "provider-exclusive",
                "Provider Sandbox",
                "Sandbox");

        var productionAccount =
            await CreateProviderAccountAsync(
                client,
                setup.Tenant,
                "provider-exclusive",
                "Provider Production",
                "Production");

        await ConfigureCredentialsAsync(
            client,
            setup.Tenant,
            sandboxAccount.AccountId,
            "sandbox-secret");

        await ConfigureCredentialsAsync(
            client,
            setup.Tenant,
            productionAccount.AccountId,
            "production-secret");

        var firstEnable =
            await client.PutAsJsonAsync(
                ProviderAccountUrl(
                    setup.Tenant,
                    sandboxAccount.AccountId,
                    "state"),
                new SetPaymentProviderAccountStateRequest(
                    true));

        Assert.Equal(
            HttpStatusCode.OK,
            firstEnable.StatusCode);

        var secondEnable =
            await client.PutAsJsonAsync(
                ProviderAccountUrl(
                    setup.Tenant,
                    productionAccount.AccountId,
                    "state"),
                new SetPaymentProviderAccountStateRequest(
                    true));

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondEnable.StatusCode);
    }

    [Fact]
    public async Task ProviderAccounts_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            Tenant.Create(
                "Unauthenticated Provider Store",
                $"unauth-provider-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await tenantRepository.AddAsync(
            tenant);

        client.DefaultRequestHeaders.Authorization =
            null;

        var response =
            await client.GetAsync(
                ProviderAccountsUrl(
                    tenant));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Theory]
    [InlineData(TenantRole.Owner)]
    [InlineData(TenantRole.Admin)]
    public async Task PaymentAdministration_MfaOwnerOrAdmin_ReturnsOk(
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
                ProviderAccountsUrl(
                    setup.Tenant));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Theory]
    [InlineData(TenantRole.Manager)]
    [InlineData(TenantRole.Staff)]
    public async Task PaymentAdministration_ManagerOrStaff_ReturnsForbidden(
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
                ProviderAccountsUrl(
                    setup.Tenant));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task PaymentAdministration_PasswordOnlyOwner_ReturnsForbidden()
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
                ProviderAccountsUrl(
                    setup.Tenant));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task PaymentAdministration_WithoutMembership_ReturnsForbidden()
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
                ProviderAccountsUrl(
                    setup.Tenant));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    private static async Task<ProviderAccountSetup> CreateSetupAsync(
        MarketApiFactory factory,
        TenantRole? role)
    {
        using var client =
            factory.CreateClient();

        var email =
            $"payment-admin-{Guid.NewGuid():N}@example.com";

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
            factory.Services
                .GetRequiredService<
                    IUserRepository>();

        var user =
            await userRepository.GetByIdAsync(
                userId);

        Assert.NotNull(
            user);

        var tenant =
            Tenant.Create(
                "Payment Administration Store",
                $"payment-admin-{Guid.NewGuid():N}",
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

        return new ProviderAccountSetup(
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

    private static async Task<PaymentProviderAccountResponse>
        CreateProviderAccountAsync(
            HttpClient client,
            Tenant tenant,
            string providerCode,
            string displayName,
            string environment)
    {
        var response =
            await client.PostAsJsonAsync(
                ProviderAccountsUrl(
                    tenant),
                new CreatePaymentProviderAccountRequest(
                    providerCode,
                    displayName,
                    environment));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    PaymentProviderAccountResponse>();

        Assert.NotNull(
            result);

        return result;
    }

    private static async Task ConfigureCredentialsAsync(
        HttpClient client,
        Tenant tenant,
        Guid accountId,
        string secret)
    {
        var response =
            await client.PutAsJsonAsync(
                ProviderAccountUrl(
                    tenant,
                    accountId,
                    "credentials"),
                new UpdatePaymentProviderCredentialsRequest(
                    new Dictionary<string, string>
                    {
                        ["api_key"] =
                            secret
                    }));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    private static string ProviderAccountsUrl(
        Tenant tenant)
    {
        return
            $"/api/tenants/{tenant.Id.Value}/payments/provider-accounts";
    }

    private static string ProviderAccountUrl(
        Tenant tenant,
        Guid accountId,
        string operation)
    {
        return
            $"/api/tenants/{tenant.Id.Value}/payments/provider-accounts/{accountId}/{operation}";
    }

    private sealed record ProviderAccountSetup(
        User User,
        Tenant Tenant,
        TenantMembership? Membership);
}