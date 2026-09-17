using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using CatalogInventory = OFOQ.Market.Domain.Catalog.Inventory;


namespace OFOQ.Market.Application.Catalog.Products.CreateProduct;

public sealed class CreateProductHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly IProductVariantRepository
        _variantRepository;

    private readonly ICategoryRepository
        _categoryRepository;

    private readonly IProductImageRepository
        _imageRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public CreateProductHandler(
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        ICategoryRepository categoryRepository,
        IProductImageRepository imageRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _productRepository = productRepository;
        _variantRepository = variantRepository;
        _categoryRepository = categoryRepository;
        _imageRepository = imageRepository;
        _currentTenant = currentTenant;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<ProductResult> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to create a product.");
        }

        var tenantId =
            _currentTenant.TenantId.Value;

        var slugExists =
            await _productRepository
                .SlugExistsAsync(
                    command.Slug,
                    excludingProductId: null,
                    cancellationToken);

        if (slugExists)
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

            /*
             * Repository is tenant-scoped.
             *
             * A Category from another tenant therefore appears
             * exactly like a nonexistent Category.
             */
            if (category is null)
            {
                throw new ProductCategoryNotFoundException(
                    command.CategoryId.Value);
            }
        }

        var sku =
            ProductSku.Create(
                command.Sku);

        var skuExists =
            await _variantRepository
                .SkuExistsAsync(
                    sku,
                    excludingVariantId: null,
                    cancellationToken);

        if (skuExists)
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

        var inventory =
        CatalogInventory.Create(
            command.TrackInventory,
            command.Quantity,
            command.LowStockThreshold,
            command.ContinueSellingWhenOutOfStock);

        var now =
            _timeProvider.GetUtcNow();

        var product =
            Product.Create(
                tenantId,
                command.Name,
                command.Slug,
                price,
                now,
                categoryId:
                    command.CategoryId,
                description:
                    command.Description,
                compareAtPrice:
                    compareAtPrice,
                createdByUserId:
                    command.ActorUserId.Value);

        /*
         * Even products without selectable options receive one
         * default variant.
         *
         * This gives every sellable product one SKU and one
         * inventory record from day one.
         */
        var defaultVariant =
            ProductVariant.Create(
                tenantId,
                product.Id,
                "Default",
                sku,
                currency,
                inventory,
                now,
                priceOverride: null,
                isDefault: true,
                createdByUserId:
                    command.ActorUserId.Value);

        await _productRepository
            .AddAsync(
                product,
                cancellationToken);

        await _variantRepository
            .AddAsync(
                defaultVariant,
                cancellationToken);

        if (!string.IsNullOrWhiteSpace(
                command.PrimaryImageUrl))
        {
            var primaryImage =
                ProductImage.Create(
                    tenantId,
                    product.Id,
                    command.PrimaryImageUrl,
                    product.Name,
                    sortOrder: 0,
                    isPrimary: true,
                    now,
                    command.ActorUserId.Value);

            await _imageRepository
                .AddRangeAsync(
                    new[]
                    {
                        primaryImage
                    },
                    cancellationToken);
        }

        /*
         * One SaveChanges means Product + initial Variant + optional
         * primary image are persisted atomically by EF Core.
         */
        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Map(
            product,
            new[]
            {
                defaultVariant
            });
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