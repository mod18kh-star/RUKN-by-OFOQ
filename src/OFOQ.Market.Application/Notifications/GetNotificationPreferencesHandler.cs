using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Application.Notifications;

public sealed class GetNotificationPreferencesHandler
{
    private readonly ITenantNotificationPreferencesRepository _repository;
    private readonly ICurrentTenant _currentTenant;

    public GetNotificationPreferencesHandler(
        ITenantNotificationPreferencesRepository repository,
        ICurrentTenant currentTenant)
    {
        _repository = repository;
        _currentTenant = currentTenant;
    }

    public async Task<NotificationPreferencesResult> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureTenant();

        var preferences =
            await _repository.GetAsync(
                cancellationToken);

        return preferences is null
            ? new NotificationPreferencesResult(
                true,
                true,
                true,
                true)
            : new NotificationPreferencesResult(
                preferences.NewOrderEmailEnabled,
                preferences.LowStockEmailEnabled,
                preferences.ReviewEmailEnabled,
                preferences.PlatformRequestEmailEnabled);
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "Tenant context is required.");
        }
    }
}
