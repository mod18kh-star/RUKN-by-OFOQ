namespace OFOQ.Market.Contracts.Notifications;

public sealed record NotificationPreferencesResponse(
    bool NewOrderEmailEnabled,
    bool LowStockEmailEnabled,
    bool ReviewEmailEnabled,
    bool PlatformRequestEmailEnabled);

public sealed record UpdateNotificationPreferencesRequest(
    bool NewOrderEmailEnabled,
    bool LowStockEmailEnabled,
    bool ReviewEmailEnabled,
    bool PlatformRequestEmailEnabled);
