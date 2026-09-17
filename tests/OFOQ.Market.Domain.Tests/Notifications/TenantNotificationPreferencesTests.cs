using OFOQ.Market.Domain.Notifications;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Notifications;

public sealed class TenantNotificationPreferencesTests
{
    [Fact]
    public void Create_DefaultsAllMerchantEmailNotificationsToEnabled()
    {
        var item =
            TenantNotificationPreferences.Create(
                TenantId.New(),
                DateTimeOffset.UtcNow);

        Assert.True(item.NewOrderEmailEnabled);
        Assert.True(item.LowStockEmailEnabled);
        Assert.True(item.ReviewEmailEnabled);
        Assert.True(item.PlatformRequestEmailEnabled);
    }

    [Fact]
    public void Update_PersistsExplicitMerchantChoices()
    {
        var item =
            TenantNotificationPreferences.Create(
                TenantId.New(),
                DateTimeOffset.UtcNow);

        item.Update(
            false,
            true,
            false,
            true,
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.False(item.NewOrderEmailEnabled);
        Assert.True(item.LowStockEmailEnabled);
        Assert.False(item.ReviewEmailEnabled);
        Assert.True(item.PlatformRequestEmailEnabled);
    }
}
