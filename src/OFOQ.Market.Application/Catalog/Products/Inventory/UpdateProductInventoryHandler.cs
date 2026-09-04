using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.Inventory;

public sealed class UpdateProductInventoryHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly IProductVariantRepository
        _variantRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public UpdateProductInventoryHandler(
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _productRepository =
            productRepository;

        _variantRepository =
            variantRepository;

        _currentTenant =
            currentTenant;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<ProductResult?> HandleAsync(
        UpdateProductInventoryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        EnsureTenant();

        var product =
            await _productRepository
                .GetByIdAsync(
                    command.ProductId,
                    cancellationToken);

        if (product is null)
        {
            return null;
        }

        var variants =
            await _variantRepository
                .GetByProductIdAsync(
                    product.Id,
                    cancellationToken);

        var defaultVariant =
            variants.SingleOrDefault(
                variant =>
                    variant.IsDefault);

        if (defaultVariant is null)
        {
            throw new ProductDefaultVariantNotFoundException(
                product.Id);
        }

        var now =
            _timeProvider.GetUtcNow();

        defaultVariant.ConfigureInventory(
            command.TrackInventory,
            command.LowStockThreshold,
            command.ContinueSellingWhenOutOfStock,
            now,
            command.ActorUserId.Value);

        defaultVariant.SetStockQuantity(
            command.Quantity,
            now,
            command.ActorUserId.Value);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Map(
            product,
            variants);
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to update inventory.");
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