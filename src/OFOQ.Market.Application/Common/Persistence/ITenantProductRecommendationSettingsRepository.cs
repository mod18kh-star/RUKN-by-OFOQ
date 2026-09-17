using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantProductRecommendationSettingsRepository
{
    Task<TenantProductRecommendationSettings?> GetAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TenantProductRecommendationSettings settings,
        CancellationToken cancellationToken = default);
}
