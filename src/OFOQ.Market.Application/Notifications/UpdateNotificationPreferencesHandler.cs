using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Notifications;

namespace OFOQ.Market.Application.Notifications;

public sealed record UpdateNotificationPreferencesCommand(
    bool NewOrderEmailEnabled,
    bool LowStockEmailEnabled,
    bool ReviewEmailEnabled,
    bool PlatformRequestEmailEnabled,
    Guid? ActorUserId);

public sealed class UpdateNotificationPreferencesHandler
{
    private readonly ITenantNotificationPreferencesRepository _repository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public UpdateNotificationPreferencesHandler(
        ITenantNotificationPreferencesRepository repository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _currentTenant = currentTenant;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<NotificationPreferencesResult> HandleAsync(
        UpdateNotificationPreferencesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "Tenant context is required.");
        }

        var now = _timeProvider.GetUtcNow();

        var preferences =
            await _repository.GetAsync(
                cancellationToken);

        if (preferences is null)
        {
            preferences =
                TenantNotificationPreferences.Create(
                    _currentTenant.TenantId.Value,
                    now,
                    command.ActorUserId);

            await _repository.AddAsync(
                preferences,
                cancellationToken);
        }

        preferences.Update(
            command.NewOrderEmailEnabled,
            command.LowStockEmailEnabled,
            command.ReviewEmailEnabled,
            command.PlatformRequestEmailEnabled,
            now,
            command.ActorUserId);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new NotificationPreferencesResult(
            preferences.NewOrderEmailEnabled,
            preferences.LowStockEmailEnabled,
            preferences.ReviewEmailEnabled,
            preferences.PlatformRequestEmailEnabled);
    }
}
