using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Contracts.Identity;

namespace OFOQ.Market.Api.Tests.Identity;

public sealed class SessionLifecycleEndpointTests
{
    private const string Email =
        "session-user@example.com";

    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task Login_CreatesSession_RefreshRotates_AndLogoutRevokes()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        await RegisterAsync(client);

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginUserRequest(
                    Email,
                    Password));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        Assert.Contains(
            loginResponse.Headers.GetValues("Set-Cookie"),
            value =>
                value.Contains(
                    "rukn_refresh=",
                    StringComparison.Ordinal) &&
                value.Contains(
                    "httponly",
                    StringComparison.OrdinalIgnoreCase));

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginUserResponse>();

        Assert.NotNull(login);
        Assert.False(login.RequiresMfa);
        Assert.False(
            string.IsNullOrWhiteSpace(
                login.AccessToken));

        var loginJwt =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(
                    login.AccessToken);

        var sessionClaim =
            loginJwt.Claims.Single(
                claim => claim.Type == "sid");

        Assert.True(
            Guid.TryParse(
                sessionClaim.Value,
                out var sessionId));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var sessionsResponse =
            await client.GetAsync(
                "/api/auth/sessions");

        Assert.Equal(
            HttpStatusCode.OK,
            sessionsResponse.StatusCode);

        var sessions =
            await sessionsResponse.Content
                .ReadFromJsonAsync<
                    IReadOnlyList<UserSessionResponse>>();

        Assert.NotNull(sessions);
        Assert.Single(sessions);
        Assert.Equal(
            sessionId,
            sessions[0].SessionId);
        Assert.True(sessions[0].IsCurrent);

        client.DefaultRequestHeaders.Authorization = null;

        var refreshResponse =
            await client.PostAsync(
                "/api/auth/refresh",
                content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            refreshResponse.StatusCode);

        var refresh =
            await refreshResponse.Content
                .ReadFromJsonAsync<
                    RefreshSessionResponse>();

        Assert.NotNull(refresh);
        Assert.False(
            string.IsNullOrWhiteSpace(
                refresh.AccessToken));

        var refreshedJwt =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(
                    refresh.AccessToken);

        Assert.Equal(
            sessionId.ToString(),
            refreshedJwt.Claims
                .Single(
                    claim =>
                        claim.Type == "sid")
                .Value);

        var logoutResponse =
            await client.PostAsync(
                "/api/auth/logout",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutResponse.StatusCode);

        var refreshAfterLogout =
            await client.PostAsync(
                "/api/auth/refresh",
                content: null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            refreshAfterLogout.StatusCode);
    }

    [Fact]
    public async Task RevokeOtherSessions_RequiresMfaVerifiedSession()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        await RegisterAsync(client);

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginUserRequest(
                    Email,
                    Password));

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginUserResponse>();

        Assert.NotNull(login);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var response =
            await client.PostAsync(
                "/api/auth/sessions/revoke-others",
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
}
