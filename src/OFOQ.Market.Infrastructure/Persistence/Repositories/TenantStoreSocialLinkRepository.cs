using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class TenantStoreSocialLinkRepository :
    ITenantStoreSocialLinkRepository
{
    private readonly MarketDbContext
        _dbContext;

    public TenantStoreSocialLinkRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<TenantStoreSocialLink>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<TenantStoreSocialLink>()
            .OrderBy(
                item =>
                    item.SortOrder)
            .ThenBy(
                item =>
                    item.PlatformCode)
            .ToArrayAsync(
                cancellationToken);
    }

    public async Task AddRangeAsync(
        IReadOnlyCollection<TenantStoreSocialLink> socialLinks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            socialLinks);

        await _dbContext
            .Set<TenantStoreSocialLink>()
            .AddRangeAsync(
                socialLinks,
                cancellationToken);
    }

    public void RemoveRange(
        IReadOnlyCollection<TenantStoreSocialLink> socialLinks)
    {
        ArgumentNullException.ThrowIfNull(
            socialLinks);

        _dbContext
            .Set<TenantStoreSocialLink>()
            .RemoveRange(
                socialLinks);
    }
}
