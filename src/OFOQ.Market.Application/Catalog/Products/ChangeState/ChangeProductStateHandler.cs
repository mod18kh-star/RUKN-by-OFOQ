using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.Products.ChangeState;

public sealed class ChangeProductStateHandler
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

    public ChangeProductStateHandler(
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
        ProductId productId,
        ProductStateAction action,
        UserId actorUserId,
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
            return null;
        }

        var now =
            _timeProvider.GetUtcNow();

        switch (action)
        {
            case ProductStateAction.Publish:
                product.Publish(
                    now,
                    actorUserId.Value);
                break;

            case ProductStateAction.MoveToDraft:
                product.MoveToDraft(
                    now,
                    actorUserId.Value);
                break;

            case ProductStateAction.Archive:
                product.Archive(
                    now,
                    actorUserId.Value);
                break;

            case ProductStateAction.Show:
                product.Show(
                    now,
                    actorUserId.Value);
                break;

            case ProductStateAction.Hide:
                product.Hide(
                    now,
                    actorUserId.Value);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(action),
                    action,
                    "Unsupported product state action.");
        }

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        var variants =
            await _variantRepository
                .GetByProductIdAsync(
                    product.Id,
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
                "A tenant context is required to change product state.");
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