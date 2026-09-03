using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Contracts.Identity;

namespace OFOQ.Market.Api.Tests.Identity;

public sealed class AuthEndpointsTests
{
    [Fact]
    public async Task Register_ReturnsCreated()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    "USER@Example.COM",
                    "StrongPassword123"));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<RegisterUserResponse>();

        Assert.NotNull(result);

        Assert.NotEqual(
            Guid.Empty,
            result.UserId);

        Assert.Equal(
            "user@example.com",
            result.Email);

        Assert.Equal(
            "Active",
            result.Status);
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenEmailExists()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    "user@example.com",
                    "StrongPassword123"));

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    "USER@example.com",
                    "AnotherPassword123"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        Assert.Equal(
            "user_email_already_exists",
            json.RootElement
                .GetProperty("code")
                .GetString());
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenPasswordIsTooShort()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    "user@example.com",
                    "short"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        Assert.Equal(
            "invalid_password",
            json.RootElement
                .GetProperty("code")
                .GetString());
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenEmailIsInvalid()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    "not-an-email",
                    "StrongPassword123"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        Assert.Equal(
            "invalid_email",
            json.RootElement
                .GetProperty("code")
                .GetString());
    }
}