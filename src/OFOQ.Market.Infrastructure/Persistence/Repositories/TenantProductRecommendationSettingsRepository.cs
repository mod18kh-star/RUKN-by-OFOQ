using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class TenantProductRecommendationSettingsRepository :
    ITenantProductRecommendationSettingsRepository
{
    private readonly MarketDbContext
        _dbContext;

    public TenantProductRecommendationSettingsRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<TenantProductRecommendationSettings?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .TenantProductRecommendationSettings
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public Task AddAsync(
        TenantProductRecommendationSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            settings);

        return _dbContext
            .TenantProductRecommendationSettings
            .AddAsync(
                settings,
                cancellationToken)
            .AsTask();
    }
}
