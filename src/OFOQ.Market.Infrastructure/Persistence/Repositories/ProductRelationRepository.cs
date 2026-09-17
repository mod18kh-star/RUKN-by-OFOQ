using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class ProductRelationRepository :
    IProductRelationRepository
{
    private readonly MarketDbContext
        _dbContext;

    public ProductRelationRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<ProductRelation>>
        GetBySourceProductIdAsync(
            ProductId sourceProductId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .ProductRelations
            .Where(
                relation =>
                    relation.SourceProductId ==
                    sourceProductId)
            .OrderBy(
                relation =>
                    relation.Type)
            .ThenBy(
                relation =>
                    relation.SortOrder)
            .ThenBy(
                relation =>
                    relation.Id)
            .ToArrayAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<ProductRelation>>
        GetBySourceProductIdAndTypeAsync(
            ProductId sourceProductId,
            ProductRelationType type,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .ProductRelations
            .Where(
                relation =>
                    relation.SourceProductId ==
                    sourceProductId &&
                    relation.Type ==
                    type)
            .OrderBy(
                relation =>
                    relation.SortOrder)
            .ThenBy(
                relation =>
                    relation.Id)
            .ToArrayAsync(
                cancellationToken);
    }

    public async Task AddRangeAsync(
        IReadOnlyCollection<ProductRelation> relations,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            relations);

        await _dbContext
            .ProductRelations
            .AddRangeAsync(
                relations,
                cancellationToken);
    }

    public void RemoveRange(
        IReadOnlyCollection<ProductRelation> relations)
    {
        ArgumentNullException.ThrowIfNull(
            relations);

        _dbContext
            .ProductRelations
            .RemoveRange(
                relations);
    }
}
