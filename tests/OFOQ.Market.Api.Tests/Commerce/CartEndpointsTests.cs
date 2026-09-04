using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Commerce.Carts;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Commerce;

public sealed class CartEndpointsTests
{
    private const string Email =
        "cart-customer@example.com";

    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task GetCart_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateTenantAsync(
                factory);

        var response =
            await client.GetAsync(
                CartUrl(
                    tenant.Id));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task AddItem_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateTenantAsync(
                factory);

        var response =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    tenant.Id),
                new AddToCartRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    1));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCart_AuthenticatedCustomer_WithNoCart_ReturnsNoContent()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var user =
            await CreateUserAsync(
                factory,
                client);

        var tenant =
            await CreateTenantAsync(
                factory);

        SetAccessToken(
            factory,
            client,
            user);

        var response =
            await client.GetAsync(
                CartUrl(
                    tenant.Id));

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task AddItem_WithEmptyProductId_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var response =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    setup.Tenant.Id),
                new AddToCartRequest(
                    Guid.Empty,
                    Guid.NewGuid(),
                    1));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "invalid_product_id",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task AddItem_WithEmptyVariantId_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var response =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    setup.Tenant.Id),
                new AddToCartRequest(
                    Guid.NewGuid(),
                    Guid.Empty,
                    1));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "invalid_product_variant_id",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task AddItem_WithZeroQuantity_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var response =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    setup.Tenant.Id),
                new AddToCartRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    0));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "validation_error",
            await ReadErrorCodeAsync(
                response));
    }

    private static async Task<AuthSetup>
        CreateAuthenticatedSetupAsync(
            MarketApiFactory factory,
            HttpClient client)
    {
        var user =
            await CreateUserAsync(
                factory,
                client);

        var tenant =
            await CreateTenantAsync(
                factory);

        SetAccessToken(
            factory,
            client,
            user);

        return new AuthSetup(
            user,
            tenant);
    }

    private static async Task<User>
        CreateUserAsync(
            MarketApiFactory factory,
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

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    RegisterUserResponse>();

        Assert.NotNull(
            result);

        var repository =
            factory.Services
                .GetRequiredService<
                    IUserRepository>();

        var user =
            await repository.GetByIdAsync(
                UserId.From(
                    result.UserId));

        Assert.NotNull(
            user);

        return user;
    }

    private static async Task<Tenant>
        CreateTenantAsync(
            MarketApiFactory factory)
    {
        var tenant =
            Tenant.Create(
                "Cart Store",
                $"cart-store-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow);

        var repository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await repository.AddAsync(
            tenant);

        return tenant;
    }

    private static void SetAccessToken(
        MarketApiFactory factory,
        HttpClient client,
        User user)
    {
        var service =
            factory.Services
                .GetRequiredService<
                    IAccessTokenService>();

        var token =
            service.Create(
                user.Id,
                user.Email.Value,
                DateTimeOffset.UtcNow,
                AccessTokenAuthenticationLevel.PasswordOnly);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token.Token);
    }

    private static string CartUrl(
        TenantId tenantId)
    {
        return
            $"/api/tenants/{tenantId.Value}/cart";
    }

    private static string ItemsUrl(
        TenantId tenantId)
    {
        return
            $"{CartUrl(tenantId)}/items";
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
            .GetProperty("code")
            .GetString();
    }

    private sealed record AuthSetup(
        User User,
        Tenant Tenant);
}