using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Contracts.Commerce.Payments;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Commerce;

public sealed class PaymentProviderExecutionWebhookTests
{
    [Fact]
    public async Task ExecuteIntent_ReturnsRequiresActionAndPersistsAction()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);

        var response = await client.PostAsync(
            ExecuteUrl(setup),
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        Assert.NotNull(result);
        Assert.Equal("RequiresAction", result.Status);
        Assert.Equal("Redirect", result.ActionType);
        Assert.Equal(
            "https://payments.example.test/continue",
            result.ActionValue);

        var provider = factory.Services.GetRequiredService<FakePaymentProvider>();
        Assert.Equal(1, provider.CreatePaymentCallCount);

        var get = await client.GetAsync(
            $"/api/tenants/{setup.Tenant.Id.Value}/payments/intents/{setup.Intent.PaymentIntentId}");

        var persisted = await get.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        Assert.NotNull(persisted);
        Assert.Equal("Redirect", persisted.ActionType);
        Assert.Equal(result.ActionValue, persisted.ActionValue);
    }

    [Fact]
    public async Task ExecuteIntent_SecondCallDoesNotCallProviderAgain()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);

        var first = await client.PostAsync(ExecuteUrl(setup), null);
        var second = await client.PostAsync(ExecuteUrl(setup), null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var provider = factory.Services.GetRequiredService<FakePaymentProvider>();
        Assert.Equal(1, provider.CreatePaymentCallCount);
    }

    [Fact]
    public async Task ExecuteIntent_WhenProviderSucceeds_MarksPaymentAndOrderPaid()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);

        var provider = factory.Services.GetRequiredService<FakePaymentProvider>();
        provider.NextPaymentResult = new PaymentProviderResult(
            PaymentIntentStatus.Succeeded,
            "provider-success-1");

        var response = await client.PostAsync(
            ExecuteUrl(setup),
            null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        Assert.NotNull(result);
        Assert.Equal("Succeeded", result.Status);

        var paymentStore = factory.Services.GetRequiredService<InMemoryPaymentStore>();
        lock (paymentStore.SyncRoot)
        {
            Assert.Equal(
                PaymentStatus.Succeeded,
                Assert.Single(paymentStore.Items).Status);
        }

        var orderStore = factory.Services.GetRequiredService<InMemoryOrderStore>();
        lock (orderStore.SyncRoot)
        {
            Assert.Equal(
                OrderStatus.Paid,
                Assert.Single(orderStore.Items).Order.Status);
        }
    }

    [Fact]
    public async Task ExecuteIntent_WhenProviderThrows_LeavesIntentProcessingAndDoesNotRepeatCall()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);

        var provider = factory.Services.GetRequiredService<FakePaymentProvider>();
        provider.CreatePaymentException = new InvalidOperationException("provider timeout");

        var first = await client.PostAsync(ExecuteUrl(setup), null);
        Assert.Equal(HttpStatusCode.BadGateway, first.StatusCode);

        provider.CreatePaymentException = null;

        var second = await client.PostAsync(ExecuteUrl(setup), null);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var secondResult = await second.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        Assert.NotNull(secondResult);
        Assert.Equal("Processing", secondResult.Status);
        Assert.Equal(1, provider.CreatePaymentCallCount);
    }

    [Fact]
    public async Task Webhook_InvalidSignature_ReturnsUnauthorized()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);

        var provider = factory.Services.GetRequiredService<FakePaymentProvider>();
        provider.NextWebhookResult = new PaymentWebhookResult(
            false,
            "invalid-signature-event",
            PaymentIntentStatus.Processing,
            PaymentIntentId: PaymentIntentId.From(setup.Intent.PaymentIntentId));

        client.DefaultRequestHeaders.Authorization = null;

        var response = await PostWebhookAsync(
            client,
            setup,
            "{}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_Succeeded_IsAnonymousAndMarksOrderPaid()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);

        var provider = factory.Services.GetRequiredService<FakePaymentProvider>();
        provider.NextPaymentResult = new PaymentProviderResult(
            PaymentIntentStatus.Processing,
            "provider-processing-1");

        var execute = await client.PostAsync(ExecuteUrl(setup), null);
        Assert.Equal(HttpStatusCode.OK, execute.StatusCode);

        provider.NextWebhookResult = new PaymentWebhookResult(
            true,
            "event-success-1",
            PaymentIntentStatus.Succeeded,
            "provider-processing-1",
            PaymentIntentId.From(setup.Intent.PaymentIntentId),
            setup.Order.TotalAmount,
            setup.Order.Currency.Value);

        client.DefaultRequestHeaders.Authorization = null;

        var response = await PostWebhookAsync(client, setup, "{\"event\":\"paid\"}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaymentWebhookResponse>();
        Assert.NotNull(result);
        Assert.True(result.Applied);
        Assert.False(result.Duplicate);
        Assert.Equal("Succeeded", result.Status);

        var orderStore = factory.Services.GetRequiredService<InMemoryOrderStore>();
        lock (orderStore.SyncRoot)
        {
            Assert.Equal(
                OrderStatus.Paid,
                Assert.Single(orderStore.Items).Order.Status);
        }
    }

    [Fact]
    public async Task Webhook_SameExternalEvent_IsIdempotent()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);

        var provider = factory.Services.GetRequiredService<FakePaymentProvider>();
        provider.NextWebhookResult = new PaymentWebhookResult(
            true,
            "event-duplicate-1",
            PaymentIntentStatus.Processing,
            PaymentIntentId: PaymentIntentId.From(setup.Intent.PaymentIntentId));

        client.DefaultRequestHeaders.Authorization = null;

        var first = await PostWebhookAsync(client, setup, "{}");
        var second = await PostWebhookAsync(client, setup, "{}");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var secondResult = await second.Content.ReadFromJsonAsync<PaymentWebhookResponse>();
        Assert.NotNull(secondResult);
        Assert.True(secondResult.Duplicate);
        Assert.False(secondResult.Applied);

        var intentStore = factory.Services.GetRequiredService<InMemoryPaymentIntentStore>();
        lock (intentStore.SyncRoot)
        {
            var intent = Assert.Single(intentStore.Items).Intent;
            Assert.Single(
                intent.Transactions,
                transaction => transaction.ExternalEventId == "event-duplicate-1");
        }
    }

    [Fact]
    public async Task Webhook_AmountMismatch_ReturnsConflict()
    {
        await using var factory = new MarketApiFactory();
        using var client = factory.CreateClient();
        var setup = await CreateSetupAsync(factory, client);

        var provider = factory.Services.GetRequiredService<FakePaymentProvider>();
        provider.NextWebhookResult = new PaymentWebhookResult(
            true,
            "event-wrong-amount",
            PaymentIntentStatus.Succeeded,
            "provider-reference-x",
            PaymentIntentId.From(setup.Intent.PaymentIntentId),
            setup.Order.TotalAmount + 1m,
            setup.Order.Currency.Value);

        client.DefaultRequestHeaders.Authorization = null;

        var response = await PostWebhookAsync(client, setup, "{}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<TestSetup> CreateSetupAsync(
        MarketApiFactory factory,
        HttpClient client)
    {
        var customerUserId = UserId.New();
        var tenant = Tenant.Create(
            "Provider Execution Store",
            $"provider-execution-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow);

        var tenantRepository = factory.Services.GetRequiredService<ITenantRepository>();
        await tenantRepository.AddAsync(tenant);

        var order = Order.Create(
            tenant.Id,
            customerUserId,
            CartId.New(),
            CurrencyCode.Create("USD"),
            new[]
            {
                new OrderItemSnapshot(
                    ProductId.New(),
                    ProductVariantId.New(),
                    "Provider Product",
                    "Default",
                    $"SKU-{Guid.NewGuid():N}",
                    Money.Create(50m, "USD"),
                    1)
            },
            DateTimeOffset.UtcNow,
            customerUserId.Value);

        var orderStore = factory.Services.GetRequiredService<InMemoryOrderStore>();
        lock (orderStore.SyncRoot)
        {
            orderStore.Items.Add(new InMemoryOrderEntry(order, null));
        }

        var method = TenantPaymentMethod.Create(
            tenant.Id,
            PaymentMethodType.Electronic,
            "provider-a",
            "Provider A",
            "SY",
            "USD",
            null,
            null,
            DateTimeOffset.UtcNow,
            customerUserId.Value);

        var methodStore = factory.Services.GetRequiredService<InMemoryTenantPaymentMethodStore>();
        lock (methodStore.SyncRoot)
        {
            methodStore.Items.Add(method);
        }

        var provider = factory.Services.GetRequiredService<FakePaymentProvider>();
        provider.Reset();

        SetAccessToken(client, customerUserId);

        var createRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/tenants/{tenant.Id.Value}/orders/{order.Id.Value}/payments/intents");
        createRequest.Headers.Add("Idempotency-Key", $"provider-{Guid.NewGuid():N}");
        createRequest.Content = JsonContent.Create(
            new CreatePaymentIntentRequest(method.Id.Value));

        var createResponse = await client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var intent = await createResponse.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        Assert.NotNull(intent);

        return new TestSetup(
            customerUserId,
            tenant,
            order,
            method,
            intent);
    }

    private static string ExecuteUrl(TestSetup setup)
    {
        return $"/api/tenants/{setup.Tenant.Id.Value}/payments/intents/{setup.Intent.PaymentIntentId}/execute";
    }

    private static Task<HttpResponseMessage> PostWebhookAsync(
        HttpClient client,
        TestSetup setup,
        string body)
    {
        var content = new StringContent(
            body,
            Encoding.UTF8,
            "application/json");

        return client.PostAsync(
            $"/api/tenants/{setup.Tenant.Id.Value}/payments/webhooks/{setup.Method.Id.Value}",
            content);
    }

    private static void SetAccessToken(
        HttpClient client,
        UserId userId)
    {
        var now = DateTimeOffset.UtcNow;
        var token = TestJwtTokenFactory.Create(
            userId.Value,
            $"provider-{userId.Value:N}@example.com",
            now.AddMinutes(-1),
            now.AddMinutes(15));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private sealed record TestSetup(
        UserId CustomerUserId,
        Tenant Tenant,
        Order Order,
        TenantPaymentMethod Method,
        PaymentIntentResponse Intent);
}
