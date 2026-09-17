using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IProductContentBlockRepository
{
    Task<IReadOnlyList<ProductContentBlock>> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyCollection<ProductContentBlock> blocks,
        CancellationToken cancellationToken = default);

    void RemoveRange(
        IReadOnlyCollection<ProductContentBlock> blocks);
}
