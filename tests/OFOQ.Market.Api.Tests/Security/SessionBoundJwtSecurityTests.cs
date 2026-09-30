using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Security;

public sealed class SessionBoundJwtSecurityTests
{
    [Fact]
    public async Task SignedTokenWithoutSessionId_IsRejected()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var user =
            await CreateUserAsync(
                factory);

        var token =
            factory.Services
                .GetRequiredService<
                    IAccessTokenService>()
                .Create(
                    user.Id,
                    user.Email.Value,
                    DateTimeOffset.UtcNow,
                    AccessTokenAuthenticationLevel.PasswordOnly);

        SetBearerToken(
            client,
            token.Token);

        var response =
            await client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task SignedTokenWithUnknownSessionId_IsRejected()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var user =
            await CreateUserAsync(
                factory);

        var token =
            factory.Services
                .GetRequiredService<
                    ISessionAccessTokenService>()
                .Create(
                    user.Id,
                    user.Email.Value,
                    DateTimeOffset.UtcNow,
                    AccessTokenAuthenticationLevel.PasswordOnly,
                    UserSessionAuthenticationMethod.Password,
                    UserSessionId.New());

        SetBearerToken(
            client,
            token.Token);

        var response =
            await client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task SessionBackedToken_IsAccepted()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var user =
            await CreateUserAsync(
                factory);

        var token =
            factory.Services
                .GetRequiredService<
                    ITestAccessTokenService>()
                .Create(
                    user.Id,
                    user.Email.Value,
                    DateTimeOffset.UtcNow,
                    AccessTokenAuthenticationLevel.PasswordOnly);

        SetBearerToken(
            client,
            token.Token);

        var response =
            await client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task RevokedSessionToken_IsRejected()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var user =
            await CreateUserAsync(
                factory);

        var token =
            factory.Services
                .GetRequiredService<
                    ITestAccessTokenService>()
                .Create(
                    user.Id,
                    user.Email.Value,
                    DateTimeOffset.UtcNow,
                    AccessTokenAuthenticationLevel.MultiFactor);

        var sessionRepository =
            factory.Services
                .GetRequiredService<
                    InMemoryUserSessionRepository>();

        var session =
            Assert.Single(
                sessionRepository.Items);

        session.Revoke(
            "security_test",
            DateTimeOffset.UtcNow);

        SetBearerToken(
            client,
            token.Token);

        var response =
            await client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    private static async Task<User> CreateUserAsync(
        MarketApiFactory factory)
    {
        var user =
            User.Create(
                $"session-security-{Guid.NewGuid():N}@example.com",
                "test-password-hash",
                DateTimeOffset.UtcNow);

        await factory.Services
            .GetRequiredService<
                IUserRepository>()
            .AddAsync(
                user);

        return user;
    }

    private static void SetBearerToken(
        HttpClient client,
        string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }
}