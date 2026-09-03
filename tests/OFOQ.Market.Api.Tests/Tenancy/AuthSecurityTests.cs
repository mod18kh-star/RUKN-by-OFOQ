using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Contracts.Identity;

namespace OFOQ.Market.Api.Tests.Identity;

public sealed class AuthSecurityTests
{
    [Fact]
    public async Task Login_ReturnsOk_ForValidCredentials()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        await RegisterUserAsync(
            client);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginUserRequest(
                    "USER@example.com",
                    "StrongPassword123"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginUserResponse>();

        Assert.NotNull(result);

        Assert.NotEqual(
            Guid.Empty,
            result.UserId);

        Assert.Equal(
            "user@example.com",
            result.Email);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.AccessToken));

        Assert.True(
            result.ExpiresAtUtc >
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForWrongPassword()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        await RegisterUserAsync(
            client);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginUserRequest(
                    "user@example.com",
                    "WrongPassword123"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Equal(
            "invalid_credentials",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForUnknownEmail()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginUserRequest(
                    "missing@example.com",
                    "StrongPassword123"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Equal(
            "invalid_credentials",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task Login_DoesNotRevealWhetherEmailExists()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        await RegisterUserAsync(
            client);

        var wrongPasswordResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginUserRequest(
                    "user@example.com",
                    "WrongPassword123"));

        var unknownEmailResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginUserRequest(
                    "missing@example.com",
                    "WrongPassword123"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            wrongPasswordResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            unknownEmailResponse.StatusCode);

        var wrongPasswordBody =
            await wrongPasswordResponse.Content
                .ReadAsStringAsync();

        var unknownEmailBody =
            await unknownEmailResponse.Content
                .ReadAsStringAsync();

        Assert.Equal(
            wrongPasswordBody,
            unknownEmailBody);
    }

    [Fact]
    public async Task Me_ReturnsUnauthorized_WithoutToken()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsCurrentUser_WithValidToken()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        await RegisterUserAsync(
            client);

        var login =
            await LoginAsync(
                client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var response =
            await client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var currentUser =
            await response.Content
                .ReadFromJsonAsync<
                    CurrentUserResponse>();

        Assert.NotNull(
            currentUser);

        Assert.Equal(
            login.UserId,
            currentUser.UserId);

        Assert.Equal(
            login.Email,
            currentUser.Email);
    }

    [Fact]
    public async Task Me_RejectsTamperedToken()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        await RegisterUserAsync(
            client);

        var login =
            await LoginAsync(
                client);

        var tokenParts =
            login.AccessToken.Split('.');

        Assert.Equal(
            3,
            tokenParts.Length);

        var signature =
            tokenParts[2];

        var replacement =
            signature[0] == 'A'
                ? 'B'
                : 'A';

        tokenParts[2] =
            replacement +
            signature[1..];

        var tamperedToken =
            string.Join(
                ".",
                tokenParts);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                tamperedToken);

        var response =
            await client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Me_RejectsExpiredToken()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var now =
            DateTimeOffset.UtcNow;

        var token =
            TestJwtTokenFactory.Create(
                Guid.NewGuid(),
                "user@example.com",
                now.AddMinutes(-10),
                now.AddMinutes(-2));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        var response =
            await client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Me_RejectsTokenSignedWithWrongKey()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var now =
            DateTimeOffset.UtcNow;

        var token =
            TestJwtTokenFactory.Create(
                Guid.NewGuid(),
                "user@example.com",
                now.AddMinutes(-1),
                now.AddMinutes(10),
                useCorrectSigningKey: false);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        var response =
            await client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsTooManyRequests_AfterRateLimit()
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
                    "/api/auth/login",
                    new LoginUserRequest(
                        "missing@example.com",
                        "WrongPassword123"));
        }

        Assert.NotNull(
            response);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsTooManyRequests_AfterRateLimit()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        HttpResponseMessage? response =
            null;

        for (var attempt = 1;
             attempt <= 4;
             attempt++)
        {
            response =
                await client.PostAsJsonAsync(
                    "/api/auth/register",
                    new RegisterUserRequest(
                        $"user{attempt}@example.com",
                        "StrongPassword123"));
        }

        Assert.NotNull(
            response);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            response.StatusCode);
    }

    private static async Task RegisterUserAsync(
        HttpClient client)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    "user@example.com",
                    "StrongPassword123"));

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
                    "user@example.com",
                    "StrongPassword123"));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginUserResponse>();

        Assert.NotNull(
            result);

        return result;
    }

    private static async Task<string?> ReadErrorCodeAsync(
        HttpResponseMessage response)
    {
        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return json.RootElement
            .GetProperty("code")
            .GetString();
    }
}