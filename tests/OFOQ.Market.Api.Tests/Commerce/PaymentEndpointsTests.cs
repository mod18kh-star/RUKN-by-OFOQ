using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Contracts.Commerce.Payments;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Commerce;

public sealed class PaymentEndpointsTests
{
    [Fact]
    public async Task AvailableMethods_ReturnsOnlyMethodsValidForOrder()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client, totalAmount: 50m);

        AddMethod(factory, setup, PaymentMethodType.Electronic, "provider-a", "Provider A", 10m, 100m);
        AddMethod(factory, setup, PaymentMethodType.Electronic, "too-high", "Too High", 100m, null);
        AddMethod(factory, setup, PaymentMethodType.CashOnDelivery, "cod", "Cash", null, null);
        AddMethod(factory, setup, PaymentMethodType.ManualTransfer, "manual", "Manual", null, null);

        var response = await client.GetAsync(
            $"/api/tenants/{setup.Tenant.Id.Value}/orders/{setup.Order.Id.Value}/payments/methods");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var methods = await response.Content.ReadFromJsonAsync<PaymentMethodResponse[]>();
        Assert.NotNull(methods);
        Assert.Equal(2, methods.Length);
        Assert.Contains(methods, item => item.ProviderCode == "provider-a");
        Assert.Contains(methods, item => item.ProviderCode == "manual");
        Assert.DoesNotContain(methods, item => item.MethodType == "CashOnDelivery");
    }

    [Fact]
    public async Task CreateIntent_ManualTransferSelected_ReturnsConflict()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);
        var method = AddMethod(factory, setup);

        factory.Services.GetRequiredService<InMemoryManualOrderPaymentSelectionReader>()
            .SetManualPaymentSelected(setup.Order.Id.Value);

        var response = await PostCreateIntentAsync(
            client, setup, method.Id, "manual-transfer-already-selected");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("manual_payment_selected", error.Code);
    }

    [Fact]
    public async Task CreateIntent_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);
        var method = AddMethod(factory, setup);

        var response = await client.PostAsJsonAsync(
            CreateIntentUrl(setup),
            new CreatePaymentIntentRequest(method.Id.Value));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("payment_idempotency_key_required", error.Code);
    }

    [Fact]
    public async Task CreateIntent_UsesOrderAmountAndCreatesPendingIntent()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client, totalAmount: 75m);
        var method = AddMethod(factory, setup);

        var response = await PostCreateIntentAsync(
            client,
            setup,
            method.Id,
            "payment-create-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        Assert.NotNull(result);
        Assert.Equal(setup.Order.Id.Value, result.OrderId);
        Assert.Equal(75m, result.Amount);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("Pending", result.Status);
        Assert.False(result.IdempotentReplay);

        var paymentStore = factory.Services.GetRequiredService<InMemoryPaymentStore>();
        lock (paymentStore.SyncRoot)
        {
            var payment = Assert.Single(paymentStore.Items);
            Assert.Equal(75m, payment.Amount);
            Assert.Equal(setup.Order.Id, payment.OrderId);
        }
    }

    [Fact]
    public async Task CreateIntent_SameKey_ReplaysSameIntent()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);
        var method = AddMethod(factory, setup);

        var first = await PostCreateIntentAsync(client, setup, method.Id, "same-payment-key");
        var second = await PostCreateIntentAsync(client, setup, method.Id, "same-payment-key");

        var firstResult = await first.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        var secondResult = await second.Content.ReadFromJsonAsync<PaymentIntentResponse>();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);
        Assert.Equal(firstResult.PaymentIntentId, secondResult.PaymentIntentId);
        Assert.True(secondResult.IdempotentReplay);
        Assert.True(second.Headers.TryGetValues("Idempotency-Replayed", out var values));
        Assert.Contains("true", values);

        var store = factory.Services.GetRequiredService<InMemoryPaymentIntentStore>();
        lock (store.SyncRoot)
        {
            Assert.Single(store.Items);
        }
    }

    [Fact]
    public async Task CreateIntent_SameKeyWithDifferentMethod_ReturnsConflict()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);
        var firstMethod = AddMethod(factory, setup, providerCode: "first");
        var secondMethod = AddMethod(factory, setup, providerCode: "second");

        var first = await PostCreateIntentAsync(client, setup, firstMethod.Id, "conflicting-key");
        var second = await PostCreateIntentAsync(client, setup, secondMethod.Id, "conflicting-key");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var error = await second.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("payment_idempotency_conflict", error.Code);
    }

    [Fact]
    public async Task CreateIntent_ForAnotherCustomersOrder_ReturnsNotFound()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);
        var method = AddMethod(factory, setup);

        SetAccessToken(client, UserId.New());

        var response = await PostCreateIntentAsync(
            client,
            setup,
            method.Id,
            "other-customer-key");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateIntent_WhenElectronicPaymentsSuspended_ReturnsConflict()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);
        var method = AddMethod(factory, setup, PaymentMethodType.Electronic);

        var capability = TenantPaymentCapability.Create(
            setup.Tenant.Id,
            DateTimeOffset.UtcNow,
            setup.CustomerUserId.Value);

        capability.SuspendElectronicPayments(
            "support escalation",
            DateTimeOffset.UtcNow,
            setup.CustomerUserId.Value);

        var store = factory.Services.GetRequiredService<InMemoryTenantPaymentCapabilityStore>();
        lock (store.SyncRoot)
        {
            store.Items.Add(capability);
        }

        var response = await PostCreateIntentAsync(
            client,
            setup,
            method.Id,
            "suspended-key");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("electronic_payments_suspended", error.Code);
    }

    [Fact]
    public async Task GetIntent_ReturnsOwnedIntent()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);
        var method = AddMethod(factory, setup);

        var created = await PostCreateIntentAsync(client, setup, method.Id, "get-intent-key");
        var createdResult = await created.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        Assert.NotNull(createdResult);

        var response = await client.GetAsync(
            $"/api/tenants/{setup.Tenant.Id.Value}/payments/intents/{createdResult.PaymentIntentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        Assert.NotNull(result);
        Assert.Equal(createdResult.PaymentIntentId, result.PaymentIntentId);
    }

    [Fact]
    public async Task RetryFailedIntent_CreatesNewIntentAndPreservesOldIntent()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);
        var method = AddMethod(factory, setup);

        var created = await PostCreateIntentAsync(client, setup, method.Id, "original-key");
        var original = await created.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        Assert.NotNull(original);

        var store = factory.Services.GetRequiredService<InMemoryPaymentIntentStore>();
        PaymentIntent originalIntent;

        lock (store.SyncRoot)
        {
            originalIntent = Assert.Single(store.Items).Intent;
            originalIntent.MarkFailed(
                null,
                DateTimeOffset.UtcNow,
                setup.CustomerUserId.Value);
        }

        var retry = await PostRetryAsync(
            client,
            setup.Tenant.Id,
            PaymentIntentId.From(original.PaymentIntentId),
            "retry-key");

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var result = await retry.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(original.PaymentIntentId, result.PaymentIntentId);
        Assert.Equal(original.PaymentId, result.PaymentId);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(PaymentIntentStatus.Failed, originalIntent.Status);

        lock (store.SyncRoot)
        {
            Assert.Equal(2, store.Items.Count);
        }
    }

    private static async Task<TestSetup> CreateSetupAsync(
        MarketApiFactory factory,
        HttpClient client,
        decimal totalAmount = 50m)
    {
        var customerUserId = UserId.New();
        var tenant = Tenant.Create(
            "Payment API Store",
            $"payment-api-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow);

        var tenantRepository = factory.Services.GetRequiredService<ITenantRepository>();
        await tenantRepository.AddAsync(tenant);

        var snapshot = new OrderItemSnapshot(
            ProductId.New(),
            ProductVariantId.New(),
            "Payment Product",
            "Default",
            $"SKU-{Guid.NewGuid():N}",
            Money.Create(totalAmount, "USD"),
            1);

        var order = Order.Create(
            tenant.Id,
            customerUserId,
            CartId.New(),
            CurrencyCode.Create("USD"),
            new[] { snapshot },
            DateTimeOffset.UtcNow,
            customerUserId.Value);

        var orderStore = factory.Services.GetRequiredService<InMemoryOrderStore>();
        lock (orderStore.SyncRoot)
        {
            orderStore.Items.Add(new InMemoryOrderEntry(order, null));
        }

        SetAccessToken(client, customerUserId);

        return new TestSetup(customerUserId, tenant, order);
    }

    private static TenantPaymentMethod AddMethod(
        MarketApiFactory factory,
        TestSetup setup,
        PaymentMethodType type = PaymentMethodType.Electronic,
        string providerCode = "provider-a",
        string displayName = "Provider A",
        decimal? minimumAmount = null,
        decimal? maximumAmount = null)
    {
        var method = TenantPaymentMethod.Create(
            setup.Tenant.Id,
            type,
            providerCode,
            displayName,
            "SY",
            "USD",
            minimumAmount,
            maximumAmount,
            DateTimeOffset.UtcNow,
            setup.CustomerUserId.Value);

        var store = factory.Services.GetRequiredService<InMemoryTenantPaymentMethodStore>();
        lock (store.SyncRoot)
        {
            store.Items.Add(method);
        }

        return method;
    }

    private static Task<HttpResponseMessage> PostCreateIntentAsync(
        HttpClient client,
        TestSetup setup,
        TenantPaymentMethodId methodId,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            CreateIntentUrl(setup));

        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(
            new CreatePaymentIntentRequest(methodId.Value));

        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> PostRetryAsync(
        HttpClient client,
        TenantId tenantId,
        PaymentIntentId paymentIntentId,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/tenants/{tenantId.Value}/payments/intents/{paymentIntentId.Value}/retry");

        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request);
    }

    private static string CreateIntentUrl(TestSetup setup)
    {
        return $"/api/tenants/{setup.Tenant.Id.Value}/orders/{setup.Order.Id.Value}/payments/intents";
    }

    private static void SetAccessToken(HttpClient client, UserId userId)
    {
        var now = DateTimeOffset.UtcNow;
        var token = TestJwtTokenFactory.Create(
            userId.Value,
            $"payment-{userId.Value:N}@example.com",
            now.AddMinutes(-1),
            now.AddMinutes(15));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private sealed record TestSetup(
        UserId CustomerUserId,
        Tenant Tenant,
        Order Order);

    private sealed record ErrorResponse(
        string Code,
        string Message);
}
