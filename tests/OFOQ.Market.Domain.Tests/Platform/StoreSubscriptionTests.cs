using OFOQ.Market.Domain.Platform;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Platform;

public sealed class StoreSubscriptionTests
{
    [Fact]
    public void Create_StartsActiveWithSupportedPlan()
    {
        var subscription = StoreSubscription.Create(
            TenantId.New(),
            "PRO",
            StoreBillingCycle.Annual,
            DateTimeOffset.UtcNow);

        Assert.Equal("pro", subscription.PlanCode);
        Assert.Equal(StoreSubscriptionStatus.Active, subscription.Status);
        Assert.Equal(StoreBillingCycle.Annual, subscription.BillingCycle);
    }

    [Fact]
    public void SuspendAndActivate_ChangeStatus()
    {
        var now = DateTimeOffset.UtcNow;
        var subscription = StoreSubscription.Create(
            TenantId.New(),
            "business",
            StoreBillingCycle.Monthly,
            now);

        subscription.Suspend(now.AddMinutes(1));
        Assert.Equal(StoreSubscriptionStatus.Suspended, subscription.Status);

        subscription.Activate(now.AddMinutes(2));
        Assert.Equal(StoreSubscriptionStatus.Active, subscription.Status);
    }

    [Fact]
    public void ChangePlan_ActivatesSubscription()
    {
        var now = DateTimeOffset.UtcNow;
        var subscription = StoreSubscription.Create(
            TenantId.New(),
            "business",
            StoreBillingCycle.Monthly,
            now);

        subscription.Suspend(now.AddMinutes(1));
        subscription.ChangePlan(
            "extra",
            StoreBillingCycle.Annual,
            now.AddMinutes(2));

        Assert.Equal("extra", subscription.PlanCode);
        Assert.Equal(StoreBillingCycle.Annual, subscription.BillingCycle);
        Assert.Equal(StoreSubscriptionStatus.Active, subscription.Status);
    }

    [Fact]
    public void Create_RejectsUnknownPlan()
    {
        Assert.Throws<ArgumentException>(
            () => StoreSubscription.Create(
                TenantId.New(),
                "unknown",
                StoreBillingCycle.Monthly,
                DateTimeOffset.UtcNow));
    }
}
