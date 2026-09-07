using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Commerce.Payments;

public sealed class PaymentProviderExecutionDomainTests
{
    [Fact]
    public void PaymentIntent_ProcessingCanMoveToRequiresActionAndPersistAction()
    {
        var now = DateTimeOffset.UtcNow;
        var intent = CreateIntent(now);

        intent.MarkProcessing(
            null,
            now.AddSeconds(1));

        intent.MarkRequiresAction(
            "provider-reference",
            now.AddSeconds(2));

        intent.SetProviderAction(
            PaymentIntentActionType.Redirect,
            "https://payments.example.test/continue",
            now.AddSeconds(2));

        Assert.Equal(
            PaymentIntentStatus.RequiresAction,
            intent.Status);
        Assert.Equal(
            PaymentIntentActionType.Redirect,
            intent.ActionType);
        Assert.Equal(
            "https://payments.example.test/continue",
            intent.ActionValue);
    }

    [Fact]
    public void PaymentIntent_SuccessClearsProviderAction()
    {
        var now = DateTimeOffset.UtcNow;
        var intent = CreateIntent(now);

        intent.MarkRequiresAction(
            "provider-reference",
            now.AddSeconds(1));

        intent.SetProviderAction(
            PaymentIntentActionType.Redirect,
            "https://payments.example.test/continue",
            now.AddSeconds(1));

        intent.MarkSucceeded(
            "provider-reference",
            now.AddSeconds(2));

        Assert.Equal(PaymentIntentStatus.Succeeded, intent.Status);
        Assert.Null(intent.ActionType);
        Assert.Null(intent.ActionValue);
    }

    [Fact]
    public void Order_MarkPaid_IsIdempotent()
    {
        var now = DateTimeOffset.UtcNow;
        var customerUserId = UserId.New();
        var order = Order.Create(
            TenantId.New(),
            customerUserId,
            CartId.New(),
            CurrencyCode.Create("USD"),
            new[]
            {
                new OrderItemSnapshot(
                    ProductId.New(),
                    ProductVariantId.New(),
                    "Product",
                    "Default",
                    "SKU-PAID",
                    Money.Create(10m, "USD"),
                    1)
            },
            now,
            customerUserId.Value);

        order.MarkPaid(
            now.AddSeconds(1),
            customerUserId.Value);

        order.MarkPaid(
            now.AddSeconds(2),
            customerUserId.Value);

        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    private static PaymentIntent CreateIntent(DateTimeOffset now)
    {
        return PaymentIntent.Create(
            TenantId.New(),
            PaymentId.New(),
            TenantPaymentMethodId.New(),
            UserId.New(),
            Money.Create(10m, "USD"),
            PaymentMethodType.Electronic,
            PaymentProviderCode.Create("provider-a"),
            now);
    }
}
