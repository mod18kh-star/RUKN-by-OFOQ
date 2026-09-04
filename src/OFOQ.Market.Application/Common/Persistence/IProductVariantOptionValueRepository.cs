using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IProductVariantOptionValueRepository
{
    Task<IReadOnlyList<ProductVariantOptionValue>>
        GetByVariantIdAsync(
            ProductVariantId variantId,
            CancellationToken cancellationToken = default);

    Task<bool> ExistsForOptionAsync(
        ProductVariantId variantId,
        ProductOptionId optionId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ProductVariantOptionValue assignment,
        CancellationToken cancellationToken = default);
}