namespace OFOQ.Market.Application.Notifications;

public sealed record NotificationPreferencesResult(
    bool NewOrderEmailEnabled,
    bool LowStockEmailEnabled,
    bool ReviewEmailEnabled,
    bool PlatformRequestEmailEnabled);
