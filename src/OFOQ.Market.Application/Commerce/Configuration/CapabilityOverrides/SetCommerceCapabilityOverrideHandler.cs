using OFOQ.Market.Application.Commerce.Configuration.Common;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Application.Commerce.Configuration.CapabilityOverrides;

public sealed class SetCommerceCapabilityOverrideHandler
{
    private readonly ITenantCommerceVerticalRepository
        _verticalRepository;

    private readonly ITenantCommerceCapabilityOverrideRepository
        _overrideRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public SetCommerceCapabilityOverrideHandler(
        ITenantCommerceVerticalRepository verticalRepository,
        ITenantCommerceCapabilityOverrideRepository overrideRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _verticalRepository =
            verticalRepository;

        _overrideRepository =
            overrideRepository;

        _currentTenant =
            currentTenant;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<CommerceProfileResult> HandleAsync(
        SetCommerceCapabilityOverrideCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        CommerceConfigurationRules.EnsureActor(
            command.ActorUserId);

        CommerceConfigurationRules.EnsureCapabilityType(
            command.CapabilityType);

        var tenantId =
            CommerceConfigurationRules.GetRequiredTenantId(
                _currentTenant);

        var verticals =
            await _verticalRepository.GetAllAsync(
                cancellationToken);

        if (!verticals.Any(
                vertical =>
                    vertical.IsEnabled))
        {
            throw new CommerceConfigurationConflictException(
                "At least one enabled commerce vertical is required before capability overrides can be configured.");
        }

        var existing =
            await _overrideRepository.GetByCapabilityAsync(
                command.CapabilityType,
                cancellationToken);

        var now =
            _timeProvider.GetUtcNow();

        if (!command.IsEnabled.HasValue)
        {
            if (existing is not null)
            {
                if (existing.TenantId != tenantId)
                {
                    throw new TenantScopeViolationException(
                        "Cross-tenant commerce capability override access was blocked.");
                }

                _overrideRepository.Remove(
                    existing);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
        }
        else if (existing is null)
        {
            var capabilityOverride =
                TenantCommerceCapabilityOverride.Create(
                    tenantId,
                    command.CapabilityType,
                    command.IsEnabled.Value,
                    now,
                    command.ActorUserId);

            await _overrideRepository.AddAsync(
                capabilityOverride,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        else
        {
            if (existing.TenantId != tenantId)
            {
                throw new TenantScopeViolationException(
                    "Cross-tenant commerce capability override access was blocked.");
            }

            existing.SetEnabled(
                command.IsEnabled.Value,
                now,
                command.ActorUserId);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        var overrides =
            await _overrideRepository.GetAllAsync(
                cancellationToken);

        return CommerceConfigurationRules.MapProfile(
            verticals,
            overrides);
    }
}