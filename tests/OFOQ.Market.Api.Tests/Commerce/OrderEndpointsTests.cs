using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Commerce.Orders;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Commerce;

public sealed class OrderEndpointsTests
{
    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task Orders_WithoutAccessToken_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            Tenant.Create(
                "Orders Store",
                $"orders-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await tenantRepository.AddAsync(
            tenant);

        var response =
            await client.GetAsync(
                OrdersUrl(
                    tenant.Id));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_MfaOwner_ReturnsOrderPaymentAndCustomerInformation()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateOwnerSetupAsync(
                factory,
                client);

        var seeded =
            SeedOrder(
                factory,
                setup,
                paid: true);

        var response =
            await client.GetAsync(
                OrdersUrl(
                    setup.Tenant.Id));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var orders =
            await response.Content
                .ReadFromJsonAsync<
                    MerchantOrderSummaryResponse[]>();

        Assert.NotNull(
            orders);

        var order =
            Assert.Single(
                orders);

        Assert.Equal(
            seeded.Order.Id.Value,
            order.OrderId);

        Assert.Equal(
            setup.User.Email.Value,
            order.CustomerEmail);

        Assert.Equal(
            "Paid",
            order.OrderStatus);

        Assert.Equal(
            "Paid",
            order.PaymentStatus);

        Assert.Equal(
            "Unfulfilled",
            order.FulfillmentStatus);

        Assert.Equal(
            2,
            order.TotalQuantity);

        Assert.Equal(
            200m,
            order.TotalAmount);
    }

    [Fact]
    public async Task GetOrderById_ReturnsItemsAndTimeline()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateOwnerSetupAsync(
                factory,
                client);

        var seeded =
            SeedOrder(
                factory,
                setup,
                paid: true);

        var response =
            await client.GetAsync(
                OrderUrl(
                    setup.Tenant.Id,
                    seeded.Order.Id.Value));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var order =
            await response.Content
                .ReadFromJsonAsync<
                    MerchantOrderDetailResponse>();

        Assert.NotNull(
            order);

        Assert.Equal(
            seeded.Order.Id.Value,
            order.OrderId);

        Assert.Equal(
            setup.User.Email.Value,
            order.CustomerEmail);

        var item =
            Assert.Single(
                order.Items);

        Assert.Equal(
            seeded.Variant.Id.Value,
            item.ProductVariantId);

        Assert.Equal(
            2,
            item.Quantity);

        Assert.Equal(
            200m,
            item.LineTotal);

        Assert.Contains(
            order.Timeline,
            entry =>
                entry.Type ==
                "Created");

        Assert.Contains(
            order.Timeline,
            entry =>
                entry.Type ==
                "PaymentReceived");
    }

    [Fact]
    public async Task GetOrderById_ThroughDifferentTenant_ReturnsNotFound()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateOwnerSetupAsync(
                factory,
                client);

        var seeded =
            SeedOrder(
                factory,
                setup,
                paid: false);

        var secondTenant =
            await CreateTenantWithOwnerAsync(
                factory,
                setup.User,
                "Second Orders Store",
                $"second-orders-{Guid.NewGuid():N}");

        var response =
            await client.GetAsync(
                OrderUrl(
                    secondTenant.Id,
                    seeded.Order.Id.Value));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Lifecycle_PaidOrder_ProgressesThroughDelivery()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateOwnerSetupAsync(
                factory,
                client);

        var seeded =
            SeedOrder(
                factory,
                setup,
                paid: true);

        var confirm =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    seeded.Order.Id.Value,
                    "confirm"),
                content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            confirm.StatusCode);

        var processing =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    seeded.Order.Id.Value,
                    "processing"),
                content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            processing.StatusCode);

        var ready =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    seeded.Order.Id.Value,
                    "ready-to-ship"),
                content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            ready.StatusCode);

        var ship =
            await client.PostAsJsonAsync(
                StateUrl(
                    setup.Tenant.Id,
                    seeded.Order.Id.Value,
                    "ship"),
                new ShipOrderRequest(
                    "Aramex",
                    "TRACK-123"));

        Assert.Equal(
            HttpStatusCode.OK,
            ship.StatusCode);

        var shipped =
            await ship.Content
                .ReadFromJsonAsync<
                    OrderLifecycleResponse>();

        Assert.NotNull(
            shipped);

        Assert.Equal(
            "Processing",
            shipped.OrderStatus);

        Assert.Equal(
            "Shipped",
            shipped.FulfillmentStatus);

        Assert.Equal(
            "Aramex",
            shipped.ShippingCarrier);

        Assert.Equal(
            "TRACK-123",
            shipped.TrackingNumber);

        var inTransit =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    seeded.Order.Id.Value,
                    "in-transit"),
                content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            inTransit.StatusCode);

        var delivered =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    seeded.Order.Id.Value,
                    "deliver"),
                content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            delivered.StatusCode);

        var finalResult =
            await delivered.Content
                .ReadFromJsonAsync<
                    OrderLifecycleResponse>();

        Assert.NotNull(
            finalResult);

        Assert.Equal(
            "Fulfilled",
            finalResult.OrderStatus);

        Assert.Equal(
            "Delivered",
            finalResult.FulfillmentStatus);

        var detailResponse =
            await client.GetAsync(
                OrderUrl(
                    setup.Tenant.Id,
                    seeded.Order.Id.Value));

        var detail =
            await detailResponse.Content
                .ReadFromJsonAsync<
                    MerchantOrderDetailResponse>();

        Assert.NotNull(
            detail);

        Assert.Equal(
            "TRACK-123",
            detail.TrackingNumber);

        Assert.Contains(
            detail.Timeline,
            entry =>
                entry.Type ==
                "Confirmed");

        Assert.Contains(
            detail.Timeline,
            entry =>
                entry.Type ==
                "ProcessingStarted");

        Assert.Contains(
            detail.Timeline,
            entry =>
                entry.Type ==
                "ReadyToShip");

        Assert.Contains(
            detail.Timeline,
            entry =>
                entry.Type ==
                "Shipped");

        Assert.Contains(
            detail.Timeline,
            entry =>
                entry.Type ==
                "InTransit");

        Assert.Contains(
            detail.Timeline,
            entry =>
                entry.Type ==
                "Delivered");
    }

    [Fact]
    public async Task Processing_FromPendingOrder_ReturnsConflict()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateOwnerSetupAsync(
                factory,
                client);

        var seeded =
            SeedOrder(
                factory,
                setup,
                paid: false);

        var response =
            await client.PostAsync(
                StateUrl(
                    setup.Tenant.Id,
                    seeded.Order.Id.Value,
                    "processing"),
                content: null);

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
            "order_state_conflict",
            error.Code);
    }

    [Fact]
    public async Task Ship_WithBlankTrackingNumber_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateOwnerSetupAsync(
                factory,
                client);

        var seeded =
            SeedOrder(
                factory,
                setup,
                paid: true);

        seeded.Order.Confirm(
            DateTimeOffset.UtcNow,
            setup.User.Id.Value);

        seeded.Order.StartProcessing(
            DateTimeOffset.UtcNow,
            setup.User.Id.Value);

        seeded.Order.MarkReadyToShip(
            DateTimeOffset.UtcNow,
            setup.User.Id.Value);

        var response =
            await client.PostAsJsonAsync(
                StateUrl(
                    setup.Tenant.Id,
                    seeded.Order.Id.Value,
                    "ship"),
                new ShipOrderRequest(
                    "Aramex",
                    "   "));

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
            "order_validation_error",
            error.Code);
    }

    [Fact]
    public async Task Cancel_RestoresExactInventoryAndReplayDoesNotRestockTwice()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateOwnerSetupAsync(
                factory,
                client);

        var seeded =
            SeedOrder(
                factory,
                setup,
                paid: false,
                stockQuantity: 8,
                quantity: 3,
                recordInventoryDeduction: true);

        Assert.Equal(
            5,
            seeded.Variant.Inventory.Quantity);

        var first =
            await client.PostAsJsonAsync(
                StateUrl(
                    setup.Tenant.Id,
                    seeded.Order.Id.Value,
                    "cancel"),
                new CancelOrderRequest(
                    "Customer request"));

        Assert.Equal(
            HttpStatusCode.OK,
            first.StatusCode);

        var firstResult =
            await first.Content
                .ReadFromJsonAsync<
                    CancelOrderResponse>();

        Assert.NotNull(
            firstResult);

        Assert.False(
            firstResult.IsIdempotentReplay);

        Assert.Equal(
            "Cancelled",
            firstResult.OrderStatus);

        Assert.Equal(
            "Cancelled",
            firstResult.FulfillmentStatus);

        var restored =
            Assert.Single(
                firstResult.Inventory);

        Assert.Equal(
            3,
            restored.RestoredQuantity);

        Assert.Equal(
            8,
            restored.QuantityAfter);

        Assert.Equal(
            8,
            seeded.Variant.Inventory.Quantity);

        var second =
            await client.PostAsJsonAsync(
                StateUrl(
                    setup.Tenant.Id,
                    seeded.Order.Id.Value,
                    "cancel"),
                new CancelOrderRequest(
                    "Replay"));

        Assert.Equal(
            HttpStatusCode.OK,
            second.StatusCode);

        var secondResult =
            await second.Content
                .ReadFromJsonAsync<
                    CancelOrderResponse>();

        Assert.NotNull(
            secondResult);

        Assert.True(
            secondResult.IsIdempotentReplay);

        Assert.Equal(
            8,
            seeded.Variant.Inventory.Quantity);

        Assert.Single(
            seeded.Order.InventoryMovements,
            movement =>
                movement.Type ==
                InventoryMovementType.OrderCancellationRestock);
    }

    private static async Task<TestSetup>
        CreateOwnerSetupAsync(
            MarketApiFactory factory,
            HttpClient client)
    {
        var registrationResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    $"orders-{Guid.NewGuid():N}@example.com",
                    Password));

        Assert.Equal(
            HttpStatusCode.Created,
            registrationResponse.StatusCode);

        var registration =
            await registrationResponse.Content
                .ReadFromJsonAsync<
                    RegisterUserResponse>();

        Assert.NotNull(
            registration);

        var userRepository =
            factory.Services
                .GetRequiredService<
                    IUserRepository>();

        var user =
            await userRepository
                .GetByIdAsync(
                    UserId.From(
                        registration.UserId));

        Assert.NotNull(
            user);

        var tenant =
            await CreateTenantWithOwnerAsync(
                factory,
                user,
                "Orders Store",
                $"orders-store-{Guid.NewGuid():N}");

        SetAccessToken(
            factory,
            client,
            user);

        return new TestSetup(
            user,
            tenant);
    }

    private static async Task<Tenant>
        CreateTenantWithOwnerAsync(
            MarketApiFactory factory,
            User user,
            string name,
            string slug)
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                name,
                slug,
                now,
                user.Id.Value);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await tenantRepository.AddAsync(
            tenant);

        var membership =
            TenantMembership.Create(
                tenant.Id,
                user.Id,
                TenantRole.Owner,
                now,
                user.Id.Value);

        var membershipRepository =
            factory.Services
                .GetRequiredService<
                    ITenantMembershipRepository>();

        await membershipRepository
            .AddAsync(
                membership);

        return tenant;
    }

    private static SeededOrder SeedOrder(
        MarketApiFactory factory,
        TestSetup setup,
        bool paid,
        int stockQuantity = 10,
        int quantity = 2,
        bool recordInventoryDeduction = false)
    {
        var now =
            DateTimeOffset.UtcNow;

        var product =
            Product.Create(
                setup.Tenant.Id,
                "Order Product",
                $"order-product-{Guid.NewGuid():N}",
                Money.Create(
                    100m,
                    "SAR"),
                now,
                createdByUserId:
                    setup.User.Id.Value);

        var variant =
            ProductVariant.Create(
                setup.Tenant.Id,
                product.Id,
                "Default",
                ProductSku.Create(
                    $"ORDER-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "SAR"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: stockQuantity),
                now,
                isDefault: true,
                createdByUserId:
                    setup.User.Id.Value);

        var order =
            Order.Create(
                setup.Tenant.Id,
                setup.User.Id,
                CartId.New(),
                CurrencyCode.Create(
                    "SAR"),
                new[]
                {
                    new OrderItemSnapshot(
                        product.Id,
                        variant.Id,
                        product.Name,
                        variant.Name,
                        variant.Sku.Value,
                        Money.Create(
                            100m,
                            "SAR"),
                        quantity)
                },
                now,
                setup.User.Id.Value);

        if (recordInventoryDeduction)
        {
            var quantityBefore =
                variant.Inventory.Quantity;

            variant.DecreaseStock(
                quantity,
                now.AddSeconds(1),
                setup.User.Id.Value);

            order.RecordCheckoutInventoryDeduction(
                product.Id,
                variant.Id,
                quantityBefore,
                variant.Inventory.Quantity,
                now.AddSeconds(1),
                setup.User.Id.Value);
        }

        Payment? payment =
            null;

        if (paid)
        {
            payment =
                Payment.Create(
                    setup.Tenant.Id,
                    order.Id,
                    setup.User.Id,
                    Money.Create(
                        100m * quantity,
                        "SAR"),
                    now,
                    setup.User.Id.Value);

            payment.MarkSucceeded(
                now.AddSeconds(1),
                setup.User.Id.Value);

            order.MarkPaid(
                now.AddSeconds(1),
                setup.User.Id.Value);
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

        var orderStore =
            factory.Services
                .GetRequiredService<
                    InMemoryOrderStore>();

        lock (orderStore.SyncRoot)
        {
            orderStore.Items.Add(
                new InMemoryOrderEntry(
                    order,
                    CheckoutIdempotencyKey: null));
        }

        if (payment is not null)
        {
            var paymentStore =
                factory.Services
                    .GetRequiredService<
                        InMemoryPaymentStore>();

            lock (paymentStore.SyncRoot)
            {
                paymentStore.Items.Add(
                    payment);
            }
        }

        return new SeededOrder(
            order,
            variant);
    }

    private static void SetAccessToken(
        MarketApiFactory factory,
        HttpClient client,
        User user)
    {
        var accessTokenService =
            factory.Services
                .GetRequiredService<
                    IAccessTokenService>();

        var accessToken =
            accessTokenService.Create(
                user.Id,
                user.Email.Value,
                DateTimeOffset.UtcNow,
                AccessTokenAuthenticationLevel.MultiFactor);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken.Token);
    }

    private static string OrdersUrl(
        TenantId tenantId)
    {
        return
            $"/api/tenants/{tenantId.Value}/backoffice/orders";
    }

    private static string OrderUrl(
        TenantId tenantId,
        Guid orderId)
    {
        return
            $"{OrdersUrl(tenantId)}/{orderId}";
    }

    private static string StateUrl(
        TenantId tenantId,
        Guid orderId,
        string action)
    {
        return
            $"{OrderUrl(tenantId, orderId)}/{action}";
    }

    private sealed record TestSetup(
        User User,
        Tenant Tenant);

    private sealed record SeededOrder(
        Order Order,
        ProductVariant Variant);

    private sealed record ErrorResponse(
        string Code,
        string Message);
}