using OFOQ.Market.Application.Catalog.Products.CreateProduct;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.UpdateProduct;

public sealed class UpdateProductHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly IProductVariantRepository
        _variantRepository;

    private readonly ICategoryRepository
        _categoryRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public UpdateProductHandler(
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        ICategoryRepository categoryRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _productRepository =
            productRepository;

        _variantRepository =
            variantRepository;

        _categoryRepository =
            categoryRepository;

        _currentTenant =
            currentTenant;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<ProductResult?> HandleAsync(
        UpdateProductCommand command,
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

        if (await _productRepository
            .SlugExistsAsync(
                command.Slug,
                product.Id,
                cancellationToken))
        {
            throw new ProductSlugAlreadyExistsException(
                command.Slug);
        }

        if (command.CategoryId.HasValue)
        {
            var category =
                await _categoryRepository
                    .GetByIdAsync(
                        command.CategoryId.Value,
                        cancellationToken);

            if (category is null)
            {
                throw new ProductCategoryNotFoundException(
                    command.CategoryId.Value);
            }
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

        var sku =
            ProductSku.Create(
                command.Sku);

        if (await _variantRepository
            .SkuExistsAsync(
                sku,
                defaultVariant.Id,
                cancellationToken))
        {
            throw new ProductSkuAlreadyExistsException(
                sku.Value);
        }

        var currency =
            CurrencyCode.Create(
                command.Currency);

        var price =
            Money.Create(
                command.Price,
                currency);

        Money? compareAtPrice =
            command.CompareAtPrice.HasValue
                ? Money.Create(
                    command.CompareAtPrice.Value,
                    currency)
                : null;

        /*
         * Existing non-default variants may have their own
         * price override. A currency change must not leave
         * those variants in another currency.
         */
        var incompatibleVariant =
            variants.FirstOrDefault(
                variant =>
                    variant.PriceOverride.HasValue &&
                    variant.PriceOverride.Value.Currency !=
                    currency);

        if (incompatibleVariant is not null)
        {
            throw new ArgumentException(
                "Product currency cannot be changed while a variant has a price override in another currency.");
        }

        var now =
            _timeProvider.GetUtcNow();

        product.Rename(
            command.Name,
            now,
            command.ActorUserId.Value);

        product.ChangeSlug(
            command.Slug,
            now,
            command.ActorUserId.Value);

        product.ChangeDescription(
            command.Description,
            now,
            command.ActorUserId.Value);

        product.ChangeCategory(
            command.CategoryId,
            now,
            command.ActorUserId.Value);

        product.SetPricing(
            price,
            compareAtPrice,
            now,
            command.ActorUserId.Value);

        defaultVariant.ChangeSku(
            sku,
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
                "A tenant context is required to update a product.");
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
                    MapVariant)
                .ToArray());
    }

    private static ProductVariantResult MapVariant(
        ProductVariant variant)
    {
        return new ProductVariantResult(
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
            variant.IsEnabled);
    }
}