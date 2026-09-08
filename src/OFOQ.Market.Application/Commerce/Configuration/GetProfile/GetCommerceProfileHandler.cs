using OFOQ.Market.Application.Commerce.Configuration.Common;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Application.Commerce.Configuration.GetProfile;

public sealed class GetCommerceProfileHandler
{
    private readonly ITenantCommerceVerticalRepository
        _verticalRepository;

    private readonly ITenantCommerceCapabilityOverrideRepository
        _overrideRepository;

    private readonly ICurrentTenant
        _currentTenant;

    public GetCommerceProfileHandler(
        ITenantCommerceVerticalRepository verticalRepository,
        ITenantCommerceCapabilityOverrideRepository overrideRepository,
        ICurrentTenant currentTenant)
    {
        _verticalRepository =
            verticalRepository;

        _overrideRepository =
            overrideRepository;

        _currentTenant =
            currentTenant;
    }

    public async Task<CommerceProfileResult> HandleAsync(
        GetCommerceProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        CommerceConfigurationRules.GetRequiredTenantId(
            _currentTenant);

        var verticals =
            await _verticalRepository.GetAllAsync(
                cancellationToken);

        var overrides =
            await _overrideRepository.GetAllAsync(
                cancellationToken);

        return CommerceConfigurationRules.MapProfile(
            verticals,
            overrides);
    }
}