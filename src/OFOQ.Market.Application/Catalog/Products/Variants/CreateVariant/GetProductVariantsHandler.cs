using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.Variants.GetVariants;

public sealed class GetProductVariantsHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly IProductVariantRepository
        _variantRepository;

    private readonly IProductOptionRepository
        _optionRepository;

    private readonly IProductOptionValueRepository
        _valueRepository;

    private readonly IProductVariantOptionValueRepository
        _assignmentRepository;

    private readonly ICurrentTenant
        _currentTenant;

    public GetProductVariantsHandler(
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        IProductOptionRepository optionRepository,
        IProductOptionValueRepository valueRepository,
        IProductVariantOptionValueRepository assignmentRepository,
        ICurrentTenant currentTenant)
    {
        _productRepository =
            productRepository;

        _variantRepository =
            variantRepository;

        _optionRepository =
            optionRepository;

        _valueRepository =
            valueRepository;

        _assignmentRepository =
            assignmentRepository;

        _currentTenant =
            currentTenant;
    }

    public async Task<IReadOnlyList<StructuredProductVariantResult>>
        HandleAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
    {
        EnsureTenant();

        var product =
            await _productRepository
                .GetByIdAsync(
                    productId,
                    cancellationToken);

        if (product is null)
        {
            throw new ProductVariantProductNotFoundException(
                productId);
        }

        var options =
            await _optionRepository
                .GetByProductIdAsync(
                    product.Id,
                    cancellationToken);

        var optionById =
            options.ToDictionary(
                option =>
                    option.Id);

        var variants =
            await _variantRepository
                .GetByProductIdAsync(
                    product.Id,
                    cancellationToken);

        var results =
            new List<StructuredProductVariantResult>(
                variants.Count);

        foreach (var variant in variants)
        {
            var assignments =
                await _assignmentRepository
                    .GetByVariantIdAsync(
                        variant.Id,
                        cancellationToken);

            var selections =
                new List<ProductVariantSelectionResult>(
                    assignments.Count);

            foreach (var assignment in assignments)
            {
                var value =
                    await _valueRepository
                        .GetByIdAsync(
                            assignment.ProductOptionValueId,
                            cancellationToken);

                if (value is null)
                {
                    continue;
                }

                if (!optionById.TryGetValue(
                        assignment.ProductOptionId,
                        out var option))
                {
                    continue;
                }

                selections.Add(
                    new ProductVariantSelectionResult(
                        option.Id,
                        option.Name,
                        value.Id,
                        value.Value));
            }

            results.Add(
                new StructuredProductVariantResult(
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
                    variant.IsEnabled,
                    selections
                        .OrderBy(
                            selection =>
                                optionById[
                                    selection.OptionId]
                                    .SortOrder)
                        .ToArray()));
        }

        return results;
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to read product variants.");
        }
    }
}