using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class TenantCommerceVerticalRepository :
    ITenantCommerceVerticalRepository
{
    private readonly MarketDbContext
        _dbContext;

    public TenantCommerceVerticalRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<TenantCommerceVertical>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<TenantCommerceVertical>()
            .OrderByDescending(
                vertical =>
                    vertical.IsPrimary)
            .ThenBy(
                vertical =>
                    vertical.VerticalType)
            .ToArrayAsync(
                cancellationToken);
    }

    public Task<TenantCommerceVertical?> GetByTypeAsync(
        CommerceVerticalType verticalType,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Set<TenantCommerceVertical>()
            .SingleOrDefaultAsync(
                vertical =>
                    vertical.VerticalType ==
                    verticalType,
                cancellationToken);
    }

    public async Task AddAsync(
        TenantCommerceVertical vertical,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            vertical);

        await _dbContext
            .Set<TenantCommerceVertical>()
            .AddAsync(
                vertical,
                cancellationToken);
    }
}