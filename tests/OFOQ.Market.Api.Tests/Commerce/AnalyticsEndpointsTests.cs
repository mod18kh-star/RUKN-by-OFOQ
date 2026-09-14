using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Commerce.Analytics;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Commerce;

public sealed class AnalyticsEndpointsTests
{
    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task Summary_WithoutAccessToken_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            Tenant.Create(
                "Analytics Store",
                $"analytics-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await tenantRepository.AddAsync(
            tenant);

        var response =
            await client.GetAsync(
                SummaryUrl(
                    tenant.Id));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Summary_ReturnsCapturedSalesAndTopProducts_PerCurrency()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateOwnerSetupAsync(
                factory,
                client);

        var now =
            DateTimeOffset.UtcNow;

        SeedOrder(
            factory,
            setup.Tenant.Id,
            setup.User.Id,
            "Alpha",
            "USD",
            30m,
            2,
            now.AddDays(-10),
            now.AddDays(-9),
            SeedLifecycle.Processing);

        SeedOrder(
            factory,
            setup.Tenant.Id,
            setup.User.Id,
            "Beta",
            "USD",
            40m,
            1,
            now.AddDays(-8),
            now.AddDays(-7),
            SeedLifecycle.Paid);

        SeedOrder(
            factory,
            setup.Tenant.Id,
            setup.User.Id,
            "Gamma",
            "SAR",
            50m,
            3,
            now.AddDays(-6),
            now.AddDays(-5),
            SeedLifecycle.Cancelled);

        SeedOrder(
            factory,
            setup.Tenant.Id,
            setup.User.Id,
            "Pending High Value",
            "USD",
            999m,
            5,
            now.AddDays(-3),
            paymentSucceededAtUtc:
                null,
            SeedLifecycle.Pending);

        var secondTenant =
            await CreateTenantWithOwnerAsync(
                factory,
                setup.User,
                "Other Analytics Store",
                $"analytics-other-{Guid.NewGuid():N}");

        SeedOrder(
            factory,
            secondTenant.Id,
            setup.User.Id,
            "Other Tenant Product",
            "USD",
            5000m,
            10,
            now.AddDays(-2),
            now.AddDays(-1),
            SeedLifecycle.Paid);

        var fromUtc =
            now.AddDays(
                -30);

        var url =
            $"{SummaryUrl(setup.Tenant.Id)}" +
            $"?fromUtc={Uri.EscapeDataString(fromUtc.ToString("O"))}" +
            $"&toUtc={Uri.EscapeDataString(now.ToString("O"))}" +
            "&topProducts=1";

        var response =
            await client.GetAsync(
                url);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    MerchantAnalyticsResponse>();

        Assert.NotNull(
            result);

        Assert.Equal(
            4,
            result.TotalOrders);

        Assert.Equal(
            1,
            result.CancelledOrders);

        Assert.Equal(
            2,
            result.Sales.Count);

        var usd =
            Assert.Single(
                result.Sales,
                item =>
                    item.Currency ==
                    "USD");

        Assert.Equal(
            2,
            usd.PaidOrders);

        Assert.Equal(
            100m,
            usd.CapturedSales);

        Assert.Equal(
            50m,
            usd.AverageOrderValue);

        var sar =
            Assert.Single(
                result.Sales,
                item =>
                    item.Currency ==
                    "SAR");

        Assert.Equal(
            1,
            sar.PaidOrders);

        Assert.Equal(
            150m,
            sar.CapturedSales);

        Assert.Equal(
            150m,
            sar.AverageOrderValue);

        Assert.Equal(
            2,
            result.TopProducts.Count);

        var usdTop =
            Assert.Single(
                result.TopProducts,
                item =>
                    item.Currency ==
                    "USD");

        Assert.Equal(
            "Alpha",
            usdTop.ProductName);

        Assert.Equal(
            60m,
            usdTop.CapturedSales);

        var sarTop =
            Assert.Single(
                result.TopProducts,
                item =>
                    item.Currency ==
                    "SAR");

        Assert.Equal(
            "Gamma",
            sarTop.ProductName);

        Assert.Equal(
            150m,
            sarTop.CapturedSales);

        Assert.DoesNotContain(
            result.TopProducts,
            item =>
                item.ProductName ==
                "Pending High Value");

        Assert.DoesNotContain(
            result.TopProducts,
            item =>
                item.ProductName ==
                "Other Tenant Product");
    }

    [Fact]
    public async Task Summary_InvalidDateRange_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateOwnerSetupAsync(
                factory,
                client);

        var now =
            DateTimeOffset.UtcNow;

        var url =
            $"{SummaryUrl(setup.Tenant.Id)}" +
            $"?fromUtc={Uri.EscapeDataString(now.ToString("O"))}" +
            $"&toUtc={Uri.EscapeDataString(now.AddDays(-1).ToString("O"))}";

        var response =
            await client.GetAsync(
                url);

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
            "analytics_query_invalid",
            error.Code);
    }

    [Fact]
    public async Task Summary_InvalidTopProducts_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateOwnerSetupAsync(
                factory,
                client);

        var response =
            await client.GetAsync(
                $"{SummaryUrl(setup.Tenant.Id)}?topProducts=0");

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
            "analytics_query_invalid",
            error.Code);
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
                    $"analytics-{Guid.NewGuid():N}@example.com",
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
            await userRepository.GetByIdAsync(
                UserId.From(
                    registration.UserId));

        Assert.NotNull(
            user);

        var tenant =
            await CreateTenantWithOwnerAsync(
                factory,
                user,
                "Analytics Store",
                $"analytics-store-{Guid.NewGuid():N}");

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

        await membershipRepository.AddAsync(
            membership);

        return tenant;
    }

    private static void SeedOrder(
        MarketApiFactory factory,
        TenantId tenantId,
        UserId customerUserId,
        string productName,
        string currency,
        decimal unitPrice,
        int quantity,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? paymentSucceededAtUtc,
        SeedLifecycle lifecycle)
    {
        var order =
            Order.Create(
                tenantId,
                customerUserId,
                CartId.New(),
                CurrencyCode.Create(
                    currency),
                new[]
                {
                    new OrderItemSnapshot(
                        ProductId.New(),
                        ProductVariantId.New(),
                        productName,
                        "Default",
                        $"AN-{Guid.NewGuid():N}",
                        Money.Create(
                            unitPrice,
                            currency),
                        quantity)
                },
                createdAtUtc,
                customerUserId.Value);

        Payment? payment =
            null;

        if (paymentSucceededAtUtc.HasValue)
        {
            payment =
                Payment.Create(
                    tenantId,
                    order.Id,
                    customerUserId,
                    Money.Create(
                        order.TotalAmount,
                        currency),
                    createdAtUtc,
                    customerUserId.Value);

            payment.MarkSucceeded(
                paymentSucceededAtUtc.Value,
                customerUserId.Value);

            order.MarkPaid(
                paymentSucceededAtUtc.Value,
                customerUserId.Value);
        }

        if (lifecycle ==
            SeedLifecycle.Processing)
        {
            var paidAt =
                paymentSucceededAtUtc!.Value;

            order.Confirm(
                paidAt.AddMinutes(1),
                customerUserId.Value);

            order.StartProcessing(
                paidAt.AddMinutes(2),
                customerUserId.Value);
        }

        if (lifecycle ==
            SeedLifecycle.Cancelled)
        {
            order.Cancel(
                "Analytics cancellation",
                paymentSucceededAtUtc!.Value.AddMinutes(1),
                customerUserId.Value);
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

    private static string SummaryUrl(
        TenantId tenantId)
    {
        return
            $"/api/tenants/{tenantId.Value}/backoffice/analytics/summary";
    }

    private sealed record TestSetup(
        User User,
        Tenant Tenant);

    private sealed record ErrorResponse(
        string Code,
        string Message);

    private enum SeedLifecycle
    {
        Pending = 0,
        Paid = 1,
        Processing = 2,
        Cancelled = 3
    }
}