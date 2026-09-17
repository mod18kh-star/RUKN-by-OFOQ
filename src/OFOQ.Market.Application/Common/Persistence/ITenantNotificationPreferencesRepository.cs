using OFOQ.Market.Domain.Notifications;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantNotificationPreferencesRepository
{
    Task<TenantNotificationPreferences?> GetAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TenantNotificationPreferences preferences,
        CancellationToken cancellationToken = default);
}
