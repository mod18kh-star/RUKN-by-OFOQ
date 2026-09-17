using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class StoreReadinessQueryRepository :
    IStoreReadinessQueryRepository
{
    private readonly MarketDbContext
        _dbContext;

    public StoreReadinessQueryRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<StoreReadinessData> GetAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        var hasPrimaryVertical =
            await _dbContext
                .Set<TenantCommerceVertical>()
                .AnyAsync(
                    item =>
                        item.IsEnabled &&
                        item.IsPrimary,
                    cancellationToken);

        var productCount =
            await _dbContext
                .Set<Product>()
                .CountAsync(
                    cancellationToken);

        var publishedProductCount =
            await _dbContext
                .Set<Product>()
                .CountAsync(
                    item =>
                        item.Status ==
                            ProductStatus.Published &&
                        item.IsVisible,
                    cancellationToken);

        var visibleSocialLinkCount =
            await _dbContext
                .Set<TenantStoreSocialLink>()
                .CountAsync(
                    item =>
                        item.IsVisible,
                    cancellationToken);

        return new StoreReadinessData(
            hasPrimaryVertical,
            productCount,
            publishedProductCount,
            visibleSocialLinkCount);
    }
}
