using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IProductRelationRepository
{
    Task<IReadOnlyList<ProductRelation>> GetBySourceProductIdAsync(
        ProductId sourceProductId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductRelation>> GetBySourceProductIdAndTypeAsync(
        ProductId sourceProductId,
        ProductRelationType type,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyCollection<ProductRelation> relations,
        CancellationToken cancellationToken = default);

    void RemoveRange(
        IReadOnlyCollection<ProductRelation> relations);
}
