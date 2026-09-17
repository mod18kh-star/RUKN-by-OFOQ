using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantStorefrontPresentationRepository
{
    Task<TenantStorefrontPresentation?> GetAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TenantStorefrontPresentation presentation,
        CancellationToken cancellationToken = default);
}
