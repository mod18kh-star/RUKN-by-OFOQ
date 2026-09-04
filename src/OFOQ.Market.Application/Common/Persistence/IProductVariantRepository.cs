using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(
        ProductVariantId variantId,
        CancellationToken cancellationToken = default);

    Task<ProductVariant?> GetBySkuAsync(
        ProductSku sku,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default);

    Task<bool> SkuExistsAsync(
        ProductSku sku,
        ProductVariantId? excludingVariantId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ProductVariant variant,
        CancellationToken cancellationToken = default);
}