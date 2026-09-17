using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class ProductContentBlockRepository :
    IProductContentBlockRepository
{
    private readonly MarketDbContext
        _dbContext;

    public ProductContentBlockRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<ProductContentBlock>>
        GetByProductIdAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .ProductContentBlocks
            .Where(
                block =>
                    block.ProductId ==
                    productId)
            .OrderBy(
                block =>
                    block.SortOrder)
            .ThenBy(
                block =>
                    block.Id)
            .ToArrayAsync(
                cancellationToken);
    }

    public async Task AddRangeAsync(
        IReadOnlyCollection<ProductContentBlock> blocks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            blocks);

        await _dbContext
            .ProductContentBlocks
            .AddRangeAsync(
                blocks,
                cancellationToken);
    }

    public void RemoveRange(
        IReadOnlyCollection<ProductContentBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(
            blocks);

        _dbContext
            .ProductContentBlocks
            .RemoveRange(
                blocks);
    }
}
