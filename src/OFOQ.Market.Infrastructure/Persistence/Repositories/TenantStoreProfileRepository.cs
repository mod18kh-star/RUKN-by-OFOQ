using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class TenantStoreProfileRepository :
    ITenantStoreProfileRepository
{
    private readonly MarketDbContext
        _dbContext;

    public TenantStoreProfileRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<TenantStoreProfile?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Set<TenantStoreProfile>()
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        TenantStoreProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        await _dbContext
            .Set<TenantStoreProfile>()
            .AddAsync(
                profile,
                cancellationToken);
    }
}
