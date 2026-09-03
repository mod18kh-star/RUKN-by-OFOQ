using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Identity;

public sealed class MfaRecoveryEndpointTests
{
    private const string Email =
        "recovery-user@example.com";

    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task VerifyRecovery_WithValidCode_ReturnsMfaAccessToken()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateRecoveryLoginAsync(
                factory,
                client);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/recovery",
                new VerifyMfaRecoveryCodeRequest(
                    setup.Login.MfaChallengeToken!,
                    setup.RecoveryCode));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    VerifyMfaRecoveryCodeResponse>();

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

        var recoveryRepository =
            factory.Services
                .GetRequiredService<
                    InMemoryUserMfaRecoveryCodeRepository>();

        var recoveryCode =
            Assert.Single(
                recoveryRepository.Items);

        Assert.True(
            recoveryCode.IsUsed);

        Assert.NotNull(
            recoveryCode.UsedAtUtc);

        var challengeRepository =
            factory.Services
                .GetRequiredService<
                    InMemoryMfaLoginChallengeRepository>();

        var challenge =
            Assert.Single(
                challengeRepository.Items);

        Assert.True(
            challenge.IsConsumed);

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
    public async Task VerifyRecovery_WithWrongCode_ReturnsGenericUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateRecoveryLoginAsync(
                factory,
                client);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/recovery",
                new VerifyMfaRecoveryCodeRequest(
                    setup.Login.MfaChallengeToken!,
                    "WRONG-RECOVERY-CODE"));

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

        var recoveryRepository =
            factory.Services
                .GetRequiredService<
                    InMemoryUserMfaRecoveryCodeRepository>();

        var recoveryCode =
            Assert.Single(
                recoveryRepository.Items);

        Assert.False(
            recoveryCode.IsUsed);
    }

    [Fact]
    public async Task VerifyRecovery_ConsumedChallengeCannotBeUsedAgain()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateRecoveryLoginAsync(
                factory,
                client);

        var request =
            new VerifyMfaRecoveryCodeRequest(
                setup.Login.MfaChallengeToken!,
                setup.RecoveryCode);

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/recovery",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var secondResponse =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/recovery",
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
    public async Task VerifyRecovery_UsedRecoveryCodeCannotAuthenticateNewChallenge()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateRecoveryLoginAsync(
                factory,
                client);

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/recovery",
                new VerifyMfaRecoveryCodeRequest(
                    setup.Login.MfaChallengeToken!,
                    setup.RecoveryCode));

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var secondLogin =
            await LoginAsync(
                client);

        Assert.True(
            secondLogin.RequiresMfa);

        Assert.False(
            string.IsNullOrWhiteSpace(
                secondLogin.MfaChallengeToken));

        var reusedCodeResponse =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/recovery",
                new VerifyMfaRecoveryCodeRequest(
                    secondLogin.MfaChallengeToken!,
                    setup.RecoveryCode));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            reusedCodeResponse.StatusCode);

        Assert.Equal(
            "invalid_mfa_verification",
            await ReadErrorCodeAsync(
                reusedCodeResponse));

        var recoveryRepository =
            factory.Services
                .GetRequiredService<
                    InMemoryUserMfaRecoveryCodeRepository>();

        var recoveryCode =
            Assert.Single(
                recoveryRepository.Items);

        Assert.True(
            recoveryCode.IsUsed);
    }

    [Fact]
    public async Task VerifyRecovery_FiveWrongCodes_ExhaustChallenge()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateRecoveryLoginAsync(
                factory,
                client);

        for (var attempt = 1;
             attempt <=
             MfaLoginChallenge.MaximumFailedAttempts;
             attempt++)
        {
            var response =
                await client.PostAsJsonAsync(
                    "/api/auth/mfa/recovery",
                    new VerifyMfaRecoveryCodeRequest(
                        setup.Login.MfaChallengeToken!,
                        "WRONG-RECOVERY-CODE"));

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

        var recoveryRepository =
            factory.Services
                .GetRequiredService<
                    InMemoryUserMfaRecoveryCodeRepository>();

        var recoveryCode =
            Assert.Single(
                recoveryRepository.Items);

        Assert.False(
            recoveryCode.IsUsed);
    }

    [Fact]
    public async Task VerifyRecovery_MalformedChallenge_ReturnsGenericUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/mfa/recovery",
                new VerifyMfaRecoveryCodeRequest(
                    "NOT-A-VALID-CHALLENGE",
                    "NOT-A-RECOVERY-CODE"));

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
    public async Task VerifyRecovery_ReturnsTooManyRequests_AfterRateLimit()
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
                    "/api/auth/mfa/recovery",
                    new VerifyMfaRecoveryCodeRequest(
                        "NOT-A-VALID-CHALLENGE",
                        "NOT-A-RECOVERY-CODE"));
        }

        Assert.NotNull(
            response);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            response.StatusCode);
    }

    private static async Task<RecoveryLoginSetup>
        CreateRecoveryLoginAsync(
            MarketApiFactory factory,
            HttpClient client)
    {
        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    Email,
                    Password));

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var registration =
            await registerResponse.Content
                .ReadFromJsonAsync<
                    RegisterUserResponse>();

        Assert.NotNull(
            registration);

        var now =
            DateTimeOffset.UtcNow;

        var userId =
            UserId.From(
                registration.UserId);

        var mfa =
            UserMfa.BeginEnrollment(
                userId,
                "TEST-PROTECTED::TEST-RAW-SECRET",
                now.AddMinutes(-2),
                registration.UserId);

        mfa.ConfirmEnrollment(
            verifiedTimeStep:
                100,
            enabledAtUtc:
                now.AddMinutes(-1),
            updatedByUserId:
                registration.UserId);

        var mfaRepository =
            factory.Services
                .GetRequiredService<
                    InMemoryUserMfaRepository>();

        await mfaRepository.AddAsync(
            mfa);

        var recoveryCodeService =
            factory.Services
                .GetRequiredService<
                    IRecoveryCodeService>();

        var rawRecoveryCode =
            Assert.Single(
                recoveryCodeService.GenerateCodes(
                    1));

        var recoveryCodeHash =
            recoveryCodeService.Hash(
                rawRecoveryCode);

        var recoveryCode =
            UserMfaRecoveryCode.Create(
                mfa.Id,
                recoveryCodeHash,
                now.AddMinutes(-1),
                registration.UserId);

        var recoveryRepository =
            factory.Services
                .GetRequiredService<
                    InMemoryUserMfaRecoveryCodeRepository>();

        await recoveryRepository.AddRangeAsync(
            new[]
            {
                recoveryCode
            });

        var login =
            await LoginAsync(
                client);

        Assert.True(
            login.RequiresMfa);

        Assert.Null(
            login.AccessToken);

        Assert.False(
            string.IsNullOrWhiteSpace(
                login.MfaChallengeToken));

        return new RecoveryLoginSetup(
            login,
            rawRecoveryCode);
    }

    private static async Task<LoginUserResponse>
        LoginAsync(
            HttpClient client)
    {
        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginUserRequest(
                    Email,
                    Password));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<
                    LoginUserResponse>();

        Assert.NotNull(
            login);

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

    private sealed record RecoveryLoginSetup(
        LoginUserResponse Login,
        string RecoveryCode);
}