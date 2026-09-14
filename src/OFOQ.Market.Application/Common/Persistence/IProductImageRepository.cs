using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IProductImageRepository
{
    Task<IReadOnlyList<ProductImage>> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyCollection<ProductImage> images,
        CancellationToken cancellationToken = default);

    void RemoveRange(
        IReadOnlyCollection<ProductImage> images);
}