using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class TenantStorefrontPresentationRepository :
    ITenantStorefrontPresentationRepository
{
    private readonly MarketDbContext
        _dbContext;

    public TenantStorefrontPresentationRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<TenantStorefrontPresentation?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Set<TenantStorefrontPresentation>()
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        TenantStorefrontPresentation presentation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            presentation);

        await _dbContext
            .Set<TenantStorefrontPresentation>()
            .AddAsync(
                presentation,
                cancellationToken);
    }
}
