using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Commerce.Payments;

public sealed class PaymentFoundationTests
{
    [Fact]
    public void Payment_CreateAndSucceed_PreservesOrderAmountAndCurrency()
    {
        var tenantId = TenantId.New();
        var customerUserId = UserId.New();
        var now = DateTimeOffset.UtcNow;

        var payment = Payment.Create(
            tenantId,
            OrderId.New(),
            customerUserId,
            Money.Create(500_000m, "SYP"),
            now,
            customerUserId.Value);

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(500_000m, payment.Amount);
        Assert.Equal("SYP", payment.Currency.Value);

        payment.MarkSucceeded(now.AddMinutes(1), customerUserId.Value);

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Throws<InvalidOperationException>(() =>
            payment.Cancel(now.AddMinutes(2), customerUserId.Value));
    }

    [Fact]
    public void PaymentIntent_OnlyAllowsControlledTransitions()
    {
        var tenantId = TenantId.New();
        var customerUserId = UserId.New();
        var now = DateTimeOffset.UtcNow;

        var intent = PaymentIntent.Create(
            tenantId,
            PaymentId.New(),
            TenantPaymentMethodId.New(),
            customerUserId,
            Money.Create(100m, "USD"),
            PaymentMethodType.Electronic,
            PaymentProviderCode.Create("sham-cash"),
            now,
            customerUserId.Value);

        intent.MarkRequiresAction(
            "provider-1",
            now.AddSeconds(1),
            customerUserId.Value);

        intent.MarkProcessing(
            "provider-1",
            now.AddSeconds(2),
            customerUserId.Value);

        intent.MarkSucceeded(
            "provider-1",
            now.AddSeconds(3),
            customerUserId.Value);

        Assert.Equal(PaymentIntentStatus.Succeeded, intent.Status);
        Assert.Equal("provider-1", intent.ProviderReference);

        Assert.Throws<InvalidOperationException>(() =>
            intent.MarkFailed(
                "provider-1",
                now.AddSeconds(4),
                customerUserId.Value));
    }

    [Fact]
    public void TenantPaymentMethod_ControlsAvailabilityAndAmountLimits()
    {
        var method = TenantPaymentMethod.Create(
            TenantId.New(),
            PaymentMethodType.Electronic,
            "syriatel-cash",
            "Syriatel Cash",
            "SY",
            "SYP",
            10_000m,
            2_000_000m,
            DateTimeOffset.UtcNow);

        Assert.True(method.IsEnabled);
        Assert.True(method.SupportsAmount(500_000m));
        Assert.False(method.SupportsAmount(5_000m));
        Assert.False(method.SupportsAmount(3_000_000m));

        method.Disable(DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.False(method.IsEnabled);
    }

    [Fact]
    public void TenantPaymentCapability_SuspendAndResume_IsAuditable()
    {
        var adminUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var capability = TenantPaymentCapability.Create(
            TenantId.New(),
            now,
            adminUserId);

        capability.SuspendElectronicPayments(
            "Customer dispute escalation",
            now.AddMinutes(1),
            adminUserId);

        Assert.False(capability.ElectronicPaymentsAllowed);
        Assert.Equal(
            ElectronicPaymentStatus.Suspended,
            capability.ElectronicPaymentsStatus);
        Assert.Equal(
            "Customer dispute escalation",
            capability.SuspensionReason);

        capability.ResumeElectronicPayments(
            now.AddMinutes(2),
            adminUserId);

        Assert.True(capability.ElectronicPaymentsAllowed);
        Assert.Null(capability.SuspensionReason);
    }

    [Fact]
    public void PaymentIntent_RecordTransaction_PreservesFinancialSnapshot()
    {
        var tenantId = TenantId.New();
        var customerUserId = UserId.New();
        var now = DateTimeOffset.UtcNow;

        var intent = PaymentIntent.Create(
            tenantId,
            PaymentId.New(),
            TenantPaymentMethodId.New(),
            customerUserId,
            Money.Create(250m, "USD"),
            PaymentMethodType.Electronic,
            PaymentProviderCode.Create("provider-a"),
            now);

        var transaction = intent.RecordTransaction(
            PaymentTransactionType.ProviderRequest,
            "provider-ref",
            "event-1",
            now.AddSeconds(1));

        Assert.Equal(250m, transaction.Amount);
        Assert.Equal("USD", transaction.Currency.Value);
        Assert.Equal(PaymentIntentStatus.Pending, transaction.ResultingStatus);
        Assert.Equal("event-1", transaction.ExternalEventId);
        Assert.Single(intent.Transactions);
    }
}
