using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Commerce.Carts;
using OFOQ.Market.Contracts.Commerce.Checkout;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Commerce;

public sealed class CheckoutEndpointsTests
{
    [Fact]
    public async Task Checkout_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client);

        var response =
            await client.PostAsync(
                CheckoutUrl(
                    setup.Tenant.Id),
                content: null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error =
            await response.Content
                .ReadFromJsonAsync<
                    ErrorResponse>();

        Assert.NotNull(
            error);

        Assert.Equal(
            "checkout_idempotency_key_required",
            error.Code);
    }

    [Fact]
    public async Task Checkout_Success_UsesAuthoritativePriceCreatesPendingOrderAndDecreasesStock()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client,
                productPrice: 10m,
                stockQuantity: 5);

        await AddToCartAsync(
            client,
            setup,
            quantity: 2);

        setup.Product.SetPricing(
            Money.Create(
                25m,
                "USD"),
            compareAtPrice: null,
            DateTimeOffset.UtcNow,
            setup.User.Id.Value);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                CheckoutUrl(
                    setup.Tenant.Id));

        request.Headers.Add(
            "Idempotency-Key",
            "api-checkout-001");

        var response =
            await client.SendAsync(
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    CheckoutResponse>();

        Assert.NotNull(
            result);

        Assert.Equal(
            "Pending",
            result.Status);

        Assert.Equal(
            50m,
            result.TotalAmount);

        Assert.False(
            result.IdempotentReplay);

        var item =
            Assert.Single(
                result.Items);

        Assert.Equal(
            25m,
            item.UnitPrice);

        Assert.Equal(
            3,
            setup.Variant.Inventory.Quantity);

        var cartStore =
            factory.Services
                .GetRequiredService<
                    InMemoryCartStore>();

        lock (cartStore.SyncRoot)
        {
            var cart =
                Assert.Single(
                    cartStore.Items);

            Assert.Equal(
                CartStatus.Converted,
                cart.Status);
        }

        var orderStore =
            factory.Services
                .GetRequiredService<
                    InMemoryOrderStore>();

        lock (orderStore.SyncRoot)
        {
            Assert.Single(
                orderStore.Items);
        }
    }

    [Fact]
    public async Task Checkout_SameIdempotencyKey_ReplaysSameOrderWithoutSecondInventoryDecrease()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client,
                productPrice: 15m,
                stockQuantity: 4);

        await AddToCartAsync(
            client,
            setup,
            quantity: 2);

        var first =
            await PostCheckoutAsync(
                client,
                setup.Tenant.Id,
                "same-key");

        var firstResult =
            await first.Content
                .ReadFromJsonAsync<
                    CheckoutResponse>();

        var second =
            await PostCheckoutAsync(
                client,
                setup.Tenant.Id,
                "same-key");

        var secondResult =
            await second.Content
                .ReadFromJsonAsync<
                    CheckoutResponse>();

        Assert.Equal(
            HttpStatusCode.OK,
            first.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            second.StatusCode);

        Assert.NotNull(
            firstResult);

        Assert.NotNull(
            secondResult);

        Assert.Equal(
            firstResult.OrderId,
            secondResult.OrderId);

        Assert.True(
            secondResult.IdempotentReplay);

        Assert.True(
            second.Headers.TryGetValues(
                "Idempotency-Replayed",
                out var replayValues));

        Assert.Contains(
            "true",
            replayValues);

        Assert.Equal(
            2,
            setup.Variant.Inventory.Quantity);
    }

    [Fact]
    public async Task Checkout_ReusingKeyForNewActiveCart_ReturnsConflict()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client,
                productPrice: 15m,
                stockQuantity: 6);

        await AddToCartAsync(
            client,
            setup,
            quantity: 1);

        var first =
            await PostCheckoutAsync(
                client,
                setup.Tenant.Id,
                "reused-key");

        Assert.Equal(
            HttpStatusCode.OK,
            first.StatusCode);

        await AddToCartAsync(
            client,
            setup,
            quantity: 1);

        var second =
            await PostCheckoutAsync(
                client,
                setup.Tenant.Id,
                "reused-key");

        Assert.Equal(
            HttpStatusCode.Conflict,
            second.StatusCode);

        var error =
            await second.Content
                .ReadFromJsonAsync<
                    ErrorResponse>();

        Assert.NotNull(
            error);

        Assert.Equal(
            "checkout_idempotency_conflict",
            error.Code);

        Assert.Equal(
            5,
            setup.Variant.Inventory.Quantity);
    }

    [Fact]
    public async Task Checkout_CannotUseCartFromAnotherTenant()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client,
                productPrice: 15m,
                stockQuantity: 4);

        await AddToCartAsync(
            client,
            setup,
            quantity: 2);

        var otherTenant =
            Tenant.Create(
                "Other Checkout Store",
                $"other-checkout-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await tenantRepository.AddAsync(
            otherTenant);

        var response =
            await PostCheckoutAsync(
                client,
                otherTenant.Id,
                "cross-tenant-key");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            4,
            setup.Variant.Inventory.Quantity);

        var cartStore =
            factory.Services
                .GetRequiredService<
                    InMemoryCartStore>();

        lock (cartStore.SyncRoot)
        {
            var cart =
                Assert.Single(
                    cartStore.Items);

            Assert.Equal(
                setup.Tenant.Id,
                cart.TenantId);

            Assert.Equal(
                CartStatus.Active,
                cart.Status);
        }
    }

    [Fact]
    public async Task Checkout_WhenInventoryIsInsufficient_ReturnsConflictAndLeavesCartActive()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client,
                productPrice: 15m,
                stockQuantity: 2);

        await AddToCartAsync(
            client,
            setup,
            quantity: 2);

        setup.Variant.SetStockQuantity(
            1,
            DateTimeOffset.UtcNow,
            setup.User.Id.Value);

        var response =
            await PostCheckoutAsync(
                client,
                setup.Tenant.Id,
                "insufficient-stock");

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
            "checkout_insufficient_stock",
            error.Code);

        var cartStore =
            factory.Services
                .GetRequiredService<
                    InMemoryCartStore>();

        lock (cartStore.SyncRoot)
        {
            var cart =
                Assert.Single(
                    cartStore.Items);

            Assert.Equal(
                CartStatus.Active,
                cart.Status);
        }

        var orderStore =
            factory.Services
                .GetRequiredService<
                    InMemoryOrderStore>();

        lock (orderStore.SyncRoot)
        {
            Assert.Empty(
                orderStore.Items);
        }
    }

    private static async Task<TestSetup> CreateSetupAsync(
        MarketApiFactory factory,
        HttpClient client,
        decimal productPrice = 10m,
        int stockQuantity = 10)
    {
        var user =
            await CreateUserAsync(
                factory,
                client);

        var tenant =
            Tenant.Create(
                "Checkout Store",
                $"checkout-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await tenantRepository.AddAsync(
            tenant);

        SetAccessToken(
            factory,
            client,
            user);

        var now =
            DateTimeOffset.UtcNow;

        var product =
            Product.Create(
                tenant.Id,
                "Checkout Product",
                $"checkout-product-{Guid.NewGuid():N}",
                Money.Create(
                    productPrice,
                    "USD"),
                now,
                createdByUserId:
                    user.Id.Value);

        product.Publish(
            now.AddSeconds(1),
            user.Id.Value);

        var variant =
            ProductVariant.Create(
                tenant.Id,
                product.Id,
                "Default",
                ProductSku.Create(
                    $"SKU-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: stockQuantity),
                now,
                isDefault:
                    true,
                createdByUserId:
                    user.Id.Value);

        var productStore =
            factory.Services
                .GetRequiredService<
                    InMemoryProductStore>();

        lock (productStore.SyncRoot)
        {
            productStore.Items.Add(
                product);
        }

        var variantStore =
            factory.Services
                .GetRequiredService<
                    InMemoryProductVariantStore>();

        lock (variantStore.SyncRoot)
        {
            variantStore.Items.Add(
                variant);
        }

        return new TestSetup(
            user,
            tenant,
            product,
            variant);
    }

    private static async Task AddToCartAsync(
        HttpClient client,
        TestSetup setup,
        int quantity)
    {
        var response =
            await client.PostAsJsonAsync(
                $"/api/tenants/{setup.Tenant.Id.Value}/cart/items",
                new AddToCartRequest(
                    setup.Product.Id.Value,
                    setup.Variant.Id.Value,
                    quantity));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    private static Task<HttpResponseMessage> PostCheckoutAsync(
        HttpClient client,
        TenantId tenantId,
        string idempotencyKey)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                CheckoutUrl(
                    tenantId));

        request.Headers.Add(
            "Idempotency-Key",
            idempotencyKey);

        return client.SendAsync(
            request);
    }

    private static async Task<User> CreateUserAsync(
        MarketApiFactory factory,
        HttpClient client)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    $"checkout-{Guid.NewGuid():N}@example.com",
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

    private static string CheckoutUrl(
        TenantId tenantId)
    {
        return
            $"/api/tenants/{tenantId.Value}/checkout";
    }

    private sealed record TestSetup(
        User User,
        Tenant Tenant,
        Product Product,
        ProductVariant Variant);

    private sealed record ErrorResponse(
        string Code,
        string Message);
}
