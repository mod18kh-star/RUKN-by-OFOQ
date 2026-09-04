 using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.GetProducts;

public sealed class GetProductsHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly IProductVariantRepository
        _variantRepository;

    private readonly ICurrentTenant
        _currentTenant;

    public GetProductsHandler(
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        ICurrentTenant currentTenant)
    {
        _productRepository = productRepository;
        _variantRepository = variantRepository;
        _currentTenant = currentTenant;
    }

    public async Task<IReadOnlyList<ProductResult>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureTenant();

        var products =
            await _productRepository
                .GetAllAsync(
                    cancellationToken);

        var results =
            new List<ProductResult>(
                products.Count);

        foreach (var product in products)
        {
            var variants =
                await _variantRepository
                    .GetByProductIdAsync(
                        product.Id,
                        cancellationToken);

            results.Add(
                Map(
                    product,
                    variants));
        }

        return results;
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to read products.");
        }
    }

    private static ProductResult Map(
        Product product,
        IReadOnlyList<ProductVariant> variants)
    {
        return new ProductResult(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.CategoryId,
            product.Price.Amount,
            product.Price.Currency.Value,
            product.CompareAtPrice?.Amount,
            product.Status,
            product.IsVisible,
            product.CreatedAtUtc,
            variants
                .Select(
                    variant =>
                        new ProductVariantResult(
                            variant.Id,
                            variant.Name,
                            variant.Sku.Value,
                            variant.IsDefault,
                            variant.PriceOverride?.Amount,
                            variant.PriceOverride?.Currency.Value,
                            variant.Inventory.TrackInventory,
                            variant.Inventory.Quantity,
                            variant.Inventory.LowStockThreshold,
                            variant.Inventory.ContinueSellingWhenOutOfStock,
                            variant.Inventory.IsAvailableForSale,
                            variant.IsEnabled))
                .ToArray());
    }
}