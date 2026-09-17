using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Application.Tenancy.StoreReadiness;

public sealed class GetStoreReadinessHandler
{
    private readonly ICurrentTenant
        _currentTenant;

    private readonly ITenantRepository
        _tenantRepository;

    private readonly ITenantStoreProfileRepository
        _profileRepository;

    private readonly IStoreReadinessQueryRepository
        _readinessQueryRepository;

    public GetStoreReadinessHandler(
        ICurrentTenant currentTenant,
        ITenantRepository tenantRepository,
        ITenantStoreProfileRepository profileRepository,
        IStoreReadinessQueryRepository readinessQueryRepository)
    {
        _currentTenant =
            currentTenant;

        _tenantRepository =
            tenantRepository;

        _profileRepository =
            profileRepository;

        _readinessQueryRepository =
            readinessQueryRepository;
    }

    public async Task<StoreReadinessResult?> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "Tenant context is required.");
        }

        var tenantId =
            _currentTenant.TenantId.Value;

        var tenant =
            await _tenantRepository
                .GetByIdAsync(
                    tenantId,
                    cancellationToken);

        if (tenant is null ||
            tenant.IsDeleted)
        {
            return null;
        }

        var profile =
            await _profileRepository
                .GetAsync(
                    cancellationToken);

        var data =
            await _readinessQueryRepository
                .GetAsync(
                    tenantId,
                    cancellationToken);

        return StoreReadinessCalculator
            .Calculate(
                tenant,
                profile,
                data);
    }
}
