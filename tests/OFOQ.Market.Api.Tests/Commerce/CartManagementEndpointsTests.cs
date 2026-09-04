using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Commerce.Carts;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Commerce;

public sealed class CartManagementEndpointsTests
{
    [Fact]
    public async Task UpdateQuantity_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateTenantAsync(
                factory,
                "Unauthorized Update");

        var response =
            await client.PutAsJsonAsync(
                ItemUrl(
                    tenant.Id,
                    Guid.NewGuid()),
                new UpdateCartItemQuantityRequest(
                    2));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task RemoveItem_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateTenantAsync(
                factory,
                "Unauthorized Remove");

        var response =
            await client.DeleteAsync(
                ItemUrl(
                    tenant.Id,
                    Guid.NewGuid()));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task ClearCart_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateTenantAsync(
                factory,
                "Unauthorized Clear");

        var response =
            await client.DeleteAsync(
                CartUrl(
                    tenant.Id));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuantity_IncreasesExistingItemQuantity()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var catalog =
            CreateProductAndVariant(
                factory,
                setup.Tenant,
                productPrice: 25m,
                quantity: 10);

        var addedCart =
            await AddItemAsync(
                client,
                setup.Tenant.Id,
                catalog,
                quantity: 2);

        var item =
            Assert.Single(
                addedCart.Items);

        var response =
            await client.PutAsJsonAsync(
                ItemUrl(
                    setup.Tenant.Id,
                    item.Id),
                new UpdateCartItemQuantityRequest(
                    4));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var cart =
            await response.Content
                .ReadFromJsonAsync<
                    CartResponse>();

        Assert.NotNull(
            cart);

        Assert.Equal(
            4,
            cart.TotalQuantity);

        Assert.Equal(
            100m,
            cart.TotalAmount);

        var updatedItem =
            Assert.Single(
                cart.Items);

        Assert.Equal(
            item.Id,
            updatedItem.Id);

        Assert.Equal(
            4,
            updatedItem.Quantity);

        Assert.Equal(
            25m,
            updatedItem.UnitPrice);

        Assert.Equal(
            100m,
            updatedItem.LineTotal);
    }

    [Fact]
    public async Task UpdateQuantity_WhenStockIsInsufficient_ReturnsConflict()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var catalog =
            CreateProductAndVariant(
                factory,
                setup.Tenant,
                productPrice: 10m,
                quantity: 3);

        var addedCart =
            await AddItemAsync(
                client,
                setup.Tenant.Id,
                catalog,
                quantity: 2);

        var item =
            Assert.Single(
                addedCart.Items);

        var response =
            await client.PutAsJsonAsync(
                ItemUrl(
                    setup.Tenant.Id,
                    item.Id),
                new UpdateCartItemQuantityRequest(
                    4));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<
                    ErrorResponse>();

        Assert.NotNull(
            error);

        Assert.Equal(
            "cart_insufficient_stock",
            error.Code);
    }

    [Fact]
    public async Task UpdateQuantity_WithUnknownItem_ReturnsNotFound()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var catalog =
            CreateProductAndVariant(
                factory,
                setup.Tenant,
                productPrice: 10m,
                quantity: 10);

        await AddItemAsync(
            client,
            setup.Tenant.Id,
            catalog,
            quantity: 1);

        var response =
            await client.PutAsJsonAsync(
                ItemUrl(
                    setup.Tenant.Id,
                    Guid.NewGuid()),
                new UpdateCartItemQuantityRequest(
                    2));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<
                    ErrorResponse>();

        Assert.NotNull(
            error);

        Assert.Equal(
            "cart_item_not_found",
            error.Code);
    }

    [Fact]
    public async Task RemoveLastItem_EmptiesCartAndResetsCurrency()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var catalog =
            CreateProductAndVariant(
                factory,
                setup.Tenant,
                productPrice: 30m,
                quantity: 10);

        var addedCart =
            await AddItemAsync(
                client,
                setup.Tenant.Id,
                catalog,
                quantity: 2);

        var item =
            Assert.Single(
                addedCart.Items);

        var response =
            await client.DeleteAsync(
                ItemUrl(
                    setup.Tenant.Id,
                    item.Id));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var cart =
            await response.Content
                .ReadFromJsonAsync<
                    CartResponse>();

        Assert.NotNull(
            cart);

        Assert.Empty(
            cart.Items);

        Assert.Equal(
            0,
            cart.TotalQuantity);

        Assert.Equal(
            0m,
            cart.TotalAmount);

        Assert.Null(
            cart.Currency);
    }

    [Fact]
    public async Task ClearCart_RemovesAllItemsAndResetsCurrency()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateAuthenticatedSetupAsync(
                factory,
                client);

        var firstCatalog =
            CreateProductAndVariant(
                factory,
                setup.Tenant,
                productPrice: 10m,
                quantity: 10);

        var secondCatalog =
            CreateProductAndVariant(
                factory,
                setup.Tenant,
                productPrice: 20m,
                quantity: 10);

        await AddItemAsync(
            client,
            setup.Tenant.Id,
            firstCatalog,
            quantity: 2);

        var cartBeforeClear =
            await AddItemAsync(
                client,
                setup.Tenant.Id,
                secondCatalog,
                quantity: 3);

        Assert.Equal(
            2,
            cartBeforeClear.Items.Count);

        Assert.Equal(
            5,
            cartBeforeClear.TotalQuantity);

        Assert.Equal(
            80m,
            cartBeforeClear.TotalAmount);

        var response =
            await client.DeleteAsync(
                CartUrl(
                    setup.Tenant.Id));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var cart =
            await response.Content
                .ReadFromJsonAsync<
                    CartResponse>();

        Assert.NotNull(
            cart);

        Assert.Empty(
            cart.Items);

        Assert.Equal(
            0,
            cart.TotalQuantity);

        Assert.Equal(
            0m,
            cart.TotalAmount);

        Assert.Null(
            cart.Currency);
    }

    [Fact]
    public async Task ClearCart_WhenNoActiveCart_ReturnsNoContent()
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
            await client.DeleteAsync(
                CartUrl(
                    setup.Tenant.Id));

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuantity_CannotAccessCartFromAnotherTenant()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var user =
            await CreateUserAsync(
                factory,
                client);

        var tenantA =
            await CreateTenantAsync(
                factory,
                "Tenant A");

        var tenantB =
            await CreateTenantAsync(
                factory,
                "Tenant B");

        SetAccessToken(
            factory,
            client,
            user);

        var catalogB =
            CreateProductAndVariant(
                factory,
                tenantB,
                productPrice: 15m,
                quantity: 10);

        var cartB =
            await AddItemAsync(
                client,
                tenantB.Id,
                catalogB,
                quantity: 2);

        var itemB =
            Assert.Single(
                cartB.Items);

        /*
         * Same authenticated customer,
         * but a different tenant route.
         *
         * Tenant A must not be able to see
         * or mutate Tenant B's cart.
         */
        var response =
            await client.PutAsJsonAsync(
                ItemUrl(
                    tenantA.Id,
                    itemB.Id),
                new UpdateCartItemQuantityRequest(
                    3));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<
                    ErrorResponse>();

        Assert.NotNull(
            error);

        Assert.Equal(
            "cart_not_found",
            error.Code);

        /*
         * Verify Tenant B's cart was not modified.
         */
        var getResponse =
            await client.GetAsync(
                CartUrl(
                    tenantB.Id));

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var unchangedCart =
            await getResponse.Content
                .ReadFromJsonAsync<
                    CartResponse>();

        Assert.NotNull(
            unchangedCart);

        Assert.Equal(
            2,
            unchangedCart.TotalQuantity);

        var unchangedItem =
            Assert.Single(
                unchangedCart.Items);

        Assert.Equal(
            2,
            unchangedItem.Quantity);
    }

    private static async Task<CartResponse> AddItemAsync(
        HttpClient client,
        TenantId tenantId,
        ProductSetup catalog,
        int quantity)
    {
        var response =
            await client.PostAsJsonAsync(
                ItemsUrl(
                    tenantId),
                new AddToCartRequest(
                    catalog.Product.Id.Value,
                    catalog.Variant.Id.Value,
                    quantity));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var cart =
            await response.Content
                .ReadFromJsonAsync<
                    CartResponse>();

        Assert.NotNull(
            cart);

        return cart;
    }

    private static ProductSetup CreateProductAndVariant(
        MarketApiFactory factory,
        Tenant tenant,
        decimal productPrice,
        int quantity)
    {
        var now =
            DateTimeOffset.UtcNow;

        var product =
            Product.Create(
                tenant.Id,
                "Cart Management Product",
                $"cart-management-{Guid.NewGuid():N}",
                Money.Create(
                    productPrice,
                    "USD"),
                now);

        product.Publish(
            now.AddSeconds(1));

        var variant =
            ProductVariant.Create(
                tenant.Id,
                product.Id,
                "Default",
                ProductSku.Create(
                    $"CART-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: quantity),
                now,
                isDefault:
                    true);

        AddProduct(
            factory,
            product);

        AddVariant(
            factory,
            variant);

        return new ProductSetup(
            product,
            variant);
    }

    private static void AddProduct(
        MarketApiFactory factory,
        Product product)
    {
        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryProductStore>();

        lock (store.SyncRoot)
        {
            store.Items.Add(
                product);
        }
    }

    private static void AddVariant(
        MarketApiFactory factory,
        ProductVariant variant)
    {
        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryProductVariantStore>();

        lock (store.SyncRoot)
        {
            store.Items.Add(
                variant);
        }
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
                factory,
                "Cart Management Store");

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
        var email =
            $"cart-management-{Guid.NewGuid():N}@example.com";

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    email,
                    "StrongPassword123"));

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
            MarketApiFactory factory,
            string name)
    {
        var tenant =
            Tenant.Create(
                name,
                $"cart-management-{Guid.NewGuid():N}",
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

    private static string ItemUrl(
        TenantId tenantId,
        Guid itemId)
    {
        return
            $"{ItemsUrl(tenantId)}/{itemId}";
    }

    private sealed record ProductSetup(
        Product Product,
        ProductVariant Variant);

    private sealed record AuthSetup(
        User User,
        Tenant Tenant);

    private sealed record ErrorResponse(
        string Code,
        string Message);
}