using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Identity;

public sealed class MfaTotpEndpointTests
{
    [Fact]
    public async Task VerifyTotp_WithValidCode_ReturnsMfaAccessToken()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var login =
            await CreateMfaChallengeAsync(
                factory,
                client);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/totp",
                new VerifyMfaTotpRequest(
                    login.MfaChallengeToken!,
                    FakeTotpService.ValidCode));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    VerifyMfaTotpResponse>();

        Assert.NotNull(
            result);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.AccessToken));

        Assert.True(
            result.ExpiresAtUtc >
            DateTimeOffset.UtcNow);

        Assert.NotNull(
            response.Headers.CacheControl);

        Assert.True(
            response.Headers.CacheControl!.NoStore);

        var jwt =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(
                    result.AccessToken);

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
                result.AccessToken);

        var meResponse =
            await client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.OK,
            meResponse.StatusCode);
    }

    [Fact]
    public async Task VerifyTotp_WithWrongCode_ReturnsGenericUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var login =
            await CreateMfaChallengeAsync(
                factory,
                client);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/totp",
                new VerifyMfaTotpRequest(
                    login.MfaChallengeToken!,
                    "000000"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Equal(
            "invalid_mfa_verification",
            await ReadErrorCodeAsync(
                response));

        var challengeRepository =
            factory.Services
                .GetRequiredService<
                    InMemoryMfaLoginChallengeRepository>();

        var challenge =
            Assert.Single(
                challengeRepository.Items);

        Assert.Equal(
            1,
            challenge.FailedAttemptCount);

        Assert.False(
            challenge.IsConsumed);
    }

    [Fact]
    public async Task VerifyTotp_ConsumedChallengeCannotBeUsedAgain()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var login =
            await CreateMfaChallengeAsync(
                factory,
                client);

        var request =
            new VerifyMfaTotpRequest(
                login.MfaChallengeToken!,
                FakeTotpService.ValidCode);

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/totp",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var secondResponse =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/totp",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            secondResponse.StatusCode);

        Assert.Equal(
            "invalid_mfa_verification",
            await ReadErrorCodeAsync(
                secondResponse));
    }

    [Fact]
    public async Task VerifyTotp_FiveWrongCodes_ExhaustChallenge()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var login =
            await CreateMfaChallengeAsync(
                factory,
                client);

        for (var attempt = 1;
             attempt <=
             MfaLoginChallenge.MaximumFailedAttempts;
             attempt++)
        {
            var response =
                await client.PostAsJsonAsync(
                    "/api/auth/mfa/totp",
                    new VerifyMfaTotpRequest(
                        login.MfaChallengeToken!,
                        "000000"));

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        var challengeRepository =
            factory.Services
                .GetRequiredService<
                    InMemoryMfaLoginChallengeRepository>();

        var challenge =
            Assert.Single(
                challengeRepository.Items);

        Assert.Equal(
            MfaLoginChallenge.MaximumFailedAttempts,
            challenge.FailedAttemptCount);

        Assert.True(
            challenge.IsExhausted);
    }

    [Fact]
    public async Task VerifyTotp_MalformedChallenge_ReturnsGenericUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/totp",
                new VerifyMfaTotpRequest(
                    "NOT-A-VALID-CHALLENGE",
                    FakeTotpService.ValidCode));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Equal(
            "invalid_mfa_verification",
            await ReadErrorCodeAsync(
                response));

        Assert.NotNull(
            response.Headers.CacheControl);

        Assert.True(
            response.Headers.CacheControl!.NoStore);
    }

    [Fact]
    public async Task VerifyTotp_ReturnsTooManyRequests_AfterRateLimit()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        HttpResponseMessage? response =
            null;

        for (var attempt = 1;
             attempt <= 6;
             attempt++)
        {
            response =
                await client.PostAsJsonAsync(
                    "/api/auth/mfa/totp",
                    new VerifyMfaTotpRequest(
                        "NOT-A-VALID-CHALLENGE",
                        "000000"));
        }

        Assert.NotNull(
            response);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            response.StatusCode);
    }

    private static async Task<LoginUserResponse>
        CreateMfaChallengeAsync(
            MarketApiFactory factory,
            HttpClient client)
    {
        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    "user@example.com",
                    "StrongPassword123"));

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var registration =
            await registerResponse.Content
                .ReadFromJsonAsync<
                    RegisterUserResponse>();

        Assert.NotNull(
            registration);

        var mfaRepository =
            factory.Services
                .GetRequiredService<
                    InMemoryUserMfaRepository>();

        var mfa =
            UserMfa.BeginEnrollment(
                UserId.From(
                    registration.UserId),
                "TEST-PROTECTED::TEST-RAW-SECRET",
                DateTimeOffset.UtcNow.AddMinutes(-2),
                registration.UserId);

        mfa.ConfirmEnrollment(
            verifiedTimeStep:
                100,
            enabledAtUtc:
                DateTimeOffset.UtcNow.AddMinutes(-1),
            updatedByUserId:
                registration.UserId);

        await mfaRepository.AddAsync(
            mfa);

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginUserRequest(
                    "user@example.com",
                    "StrongPassword123"));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<
                    LoginUserResponse>();

        Assert.NotNull(
            login);

        Assert.True(
            login.RequiresMfa);

        Assert.Null(
            login.AccessToken);

        Assert.False(
            string.IsNullOrWhiteSpace(
                login.MfaChallengeToken));

        return login;
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
}