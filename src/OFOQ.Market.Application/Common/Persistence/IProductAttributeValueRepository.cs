using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Catalog.Attributes;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IProductAttributeValueRepository
{
    Task<IReadOnlyList<ProductAttributeValue>> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ProductAttributeValue value,
        CancellationToken cancellationToken = default);

    void Remove(
        ProductAttributeValue value);
}