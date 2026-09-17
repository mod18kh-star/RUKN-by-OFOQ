using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Contracts.Identity;

namespace OFOQ.Market.Api.Tests.Identity;

public sealed class MfaEnrollmentEndpointTests
{
    private const string Email =
        "user@example.com";

    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task Enrollment_StartAndConfirm_ReturnsMfaTokenAndRecoveryCodes()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        await RegisterAsync(
            client);

        var login =
            await LoginAsync(
                client);

        Assert.False(
            login.RequiresMfa);

        Assert.False(
            string.IsNullOrWhiteSpace(
                login.AccessToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var startResponse =
            await client.PostAsync(
                "/api/auth/mfa/enrollment/start",
                content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            startResponse.StatusCode);

        Assert.NotNull(
            startResponse.Headers.CacheControl);

        Assert.True(
            startResponse.Headers.CacheControl!.NoStore);

        var start =
            await startResponse.Content
                .ReadFromJsonAsync<
                    StartMfaEnrollmentResponse>();

        Assert.NotNull(
            start);

        Assert.Equal(
            FakeTotpService.EnrollmentSecret,
            start.ManualEntryKey);

        Assert.StartsWith(
            "otpauth://totp/",
            start.ProvisioningUri);

        var confirmResponse =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/enrollment/confirm",
                new ConfirmMfaEnrollmentRequest(
                    FakeTotpService.ValidCode));

        Assert.Equal(
            HttpStatusCode.OK,
            confirmResponse.StatusCode);

        Assert.NotNull(
            confirmResponse.Headers.CacheControl);

        Assert.True(
            confirmResponse.Headers.CacheControl!.NoStore);

        var confirm =
            await confirmResponse.Content
                .ReadFromJsonAsync<
                    ConfirmMfaEnrollmentResponse>();

        Assert.NotNull(
            confirm);

        Assert.Equal(
            8,
            confirm.RecoveryCodes.Count);

        Assert.Equal(
            8,
            confirm.RecoveryCodes
                .Distinct(
                    StringComparer.Ordinal)
                .Count());

        Assert.False(
            string.IsNullOrWhiteSpace(
                confirm.AccessToken));

        var jwt =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(
                    confirm.AccessToken);

        var authenticationMethods =
            jwt.Claims
                .Where(
                    claim =>
                        claim.Type ==
                        "amr")
                .Select(
                    claim =>
                        claim.Value)
                .ToArray();

        Assert.Contains(
            "pwd",
            authenticationMethods);

        Assert.Contains(
            "mfa",
            authenticationMethods);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                confirm.AccessToken);

        var meResponse =
            await client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.OK,
            meResponse.StatusCode);

        var me =
            await meResponse.Content
                .ReadFromJsonAsync<
                    CurrentUserResponse>();

        Assert.NotNull(
            me);

        Assert.True(
            me.MfaEnabled);

        Assert.True(
            me.SessionMfaVerified);

        Assert.Contains(
            "pwd",
            me.AuthenticationMethods);

        Assert.Contains(
            "mfa",
            me.AuthenticationMethods);

        client.DefaultRequestHeaders.Authorization =
            null;

        var nextLogin =
            await LoginAsync(
                client);

        Assert.True(
            nextLogin.RequiresMfa);

        Assert.Null(
            nextLogin.AccessToken);

        Assert.False(
            string.IsNullOrWhiteSpace(
                nextLogin.MfaChallengeToken));
    }

    [Fact]
    public async Task Enrollment_ConfirmWithWrongCode_DoesNotEnableMfa()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        await RegisterAsync(
            client);

        var login =
            await LoginAsync(
                client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var startResponse =
            await client.PostAsync(
                "/api/auth/mfa/enrollment/start",
                content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            startResponse.StatusCode);

        var confirmResponse =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/enrollment/confirm",
                new ConfirmMfaEnrollmentRequest(
                    "000000"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            confirmResponse.StatusCode);

        var meResponse =
            await client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.OK,
            meResponse.StatusCode);

        var me =
            await meResponse.Content
                .ReadFromJsonAsync<
                    CurrentUserResponse>();

        Assert.NotNull(
            me);

        Assert.False(
            me.MfaEnabled);

        Assert.False(
            me.SessionMfaVerified);
    }

    [Fact]
    public async Task RecoveryCodes_Regenerate_RequiresMfaVerifiedSession()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        await RegisterAsync(
            client);

        var login =
            await LoginAsync(
                client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var response =
            await client.PostAsync(
                "/api/auth/mfa/recovery-codes/regenerate",
                content: null);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    private static async Task RegisterAsync(
        HttpClient client)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    Email,
                    Password));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);
    }

    private static async Task<LoginUserResponse> LoginAsync(
        HttpClient client)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginUserRequest(
                    Email,
                    Password));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    LoginUserResponse>();

        return Assert.IsType<
            LoginUserResponse>(
                result);
    }
}
