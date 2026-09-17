using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Tenancy.StoreReadiness;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tests.Tenancy.StoreReadiness;

public sealed class StoreReadinessCalculatorTests
{
    [Fact]
    public void Calculate_WithCompleteActiveStore_ReturnsOneHundredPercent()
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "RUKN Store",
                "rukn-store",
                now);

        tenant.Activate(
            now.AddMinutes(1));

        var profile =
            TenantStoreProfile.Create(
                tenant.Id,
                "https://example.com",
                "+966500000000",
                null,
                null,
                true,
                now);

        var result =
            StoreReadinessCalculator.Calculate(
                tenant,
                profile,
                new StoreReadinessData(
                    HasPrimaryVertical: true,
                    ProductCount: 4,
                    PublishedProductCount: 2,
                    VisibleSocialLinkCount: 1));

        Assert.Equal(
            100,
            result.Percentage);

        Assert.Equal(
            "Complete",
            result.State);

        Assert.All(
            result.Items,
            item =>
                Assert.True(
                    item.Completed));
    }

    [Fact]
    public void Calculate_DraftWithMerchantWorkComplete_ReturnsReadyForReview()
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "RUKN Store",
                "rukn-store",
                now);

        var profile =
            TenantStoreProfile.Create(
                tenant.Id,
                null,
                "+966500000000",
                null,
                null,
                true,
                now);

        var result =
            StoreReadinessCalculator.Calculate(
                tenant,
                profile,
                new StoreReadinessData(
                    HasPrimaryVertical: true,
                    ProductCount: 1,
                    PublishedProductCount: 1,
                    VisibleSocialLinkCount: 1));

        Assert.Equal(
            90,
            result.Percentage);

        Assert.Equal(
            "ReadyForReview",
            result.State);
    }
}
