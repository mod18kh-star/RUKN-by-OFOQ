using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Storefront;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Catalog.Attributes;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryStorefrontQueryRepository :
    IStorefrontQueryRepository
{
    private readonly ITenantRepository
        _tenantRepository;

    private readonly InMemoryCategoryStore
        _categoryStore;

    private readonly InMemoryProductStore
        _productStore;

    private readonly InMemoryProductVariantStore
        _variantStore;

    private readonly InMemoryTenantCommerceVerticalStore
        _verticalStore;

    private readonly InMemoryProductAttributeValueStore
        _attributeStore;

    public InMemoryStorefrontQueryRepository(
        ITenantRepository tenantRepository,
        InMemoryCategoryStore categoryStore,
        InMemoryProductStore productStore,
        InMemoryProductVariantStore variantStore,
        InMemoryTenantCommerceVerticalStore verticalStore,
        InMemoryProductAttributeValueStore attributeStore)
    {
        _tenantRepository =
            tenantRepository;

        _categoryStore =
            categoryStore;

        _productStore =
            productStore;

        _variantStore =
            variantStore;

        _verticalStore =
            verticalStore;

        _attributeStore =
            attributeStore;
    }

    public async Task<StorefrontInfoResult?> GetStoreAsync(
        TenantSlug storeSlug,
        CancellationToken cancellationToken = default)
    {
        var tenant =
            await _tenantRepository.GetBySlugAsync(
                storeSlug,
                cancellationToken);

        if (tenant is null ||
            tenant.IsDeleted ||
            tenant.Status !=
                TenantStatus.Active)
        {
            return null;
        }

        TenantCommerceVertical? vertical;

        lock (_verticalStore.SyncRoot)
        {
            vertical =
                _verticalStore.Items
                    .SingleOrDefault(
                        item =>
                            item.TenantId ==
                                tenant.Id &&
                            item.IsEnabled &&
                            item.IsPrimary);
        }

        string? verticalName =
            null;

        string? verticalCode =
            null;

        if (vertical is not null)
        {
            var definition =
                CommerceVerticalCatalog.Get(
                    vertical.VerticalType);

            verticalName =
                vertical.VerticalType.ToString();

            verticalCode =
                definition.Code;
        }

        return new StorefrontInfoResult(
            tenant.Id,
            tenant.Name,
            tenant.Slug.Value,
            verticalName,
            verticalCode);
    }

    public Task<IReadOnlyList<StorefrontCategoryResult>>
        GetCategoriesAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
    {
        lock (_categoryStore.SyncRoot)
        {
            IReadOnlyList<StorefrontCategoryResult> result =
                _categoryStore.Items
                    .Where(
                        item =>
                            item.TenantId ==
                                tenantId &&
                            !item.IsDeleted &&
                            item.IsVisible)
                    .OrderBy(
                        item =>
                            item.SortOrder)
                    .ThenBy(
                        item =>
                            item.Name)
                    .Select(
                        item =>
                            new StorefrontCategoryResult(
                                item.Id.Value,
                                item.Name,
                                item.Slug,
                                item.ParentCategoryId?.Value,
                                item.SortOrder))
                    .ToArray();

            return Task.FromResult(
                result);
        }
    }

    public Task<StorefrontProductPageResult> GetProductsAsync(
        TenantId tenantId,
        string? search,
        string? categorySlug,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        CategoryId? categoryId =
            null;

        if (!string.IsNullOrWhiteSpace(
                categorySlug))
        {
            lock (_categoryStore.SyncRoot)
            {
                var category =
                    _categoryStore.Items
                        .SingleOrDefault(
                            item =>
                                item.TenantId ==
                                    tenantId &&
                                !item.IsDeleted &&
                                item.IsVisible &&
                                item.Slug ==
                                    categorySlug);

                if (category is null)
                {
                    return Task.FromResult(
                        EmptyPage(
                            page,
                            pageSize));
                }

                categoryId =
                    category.Id;
            }
        }

        Product[] products;

        lock (_productStore.SyncRoot)
        {
            IEnumerable<Product> query =
                _productStore.Items
                    .Where(
                        item =>
                            item.TenantId ==
                                tenantId &&
                            !item.IsDeleted &&
                            item.Status ==
                                ProductStatus.Published &&
                            item.IsVisible);

            if (categoryId.HasValue)
            {
                var selectedCategoryId =
                    categoryId.Value;

                query =
                    query.Where(
                        item =>
                            item.CategoryId ==
                                selectedCategoryId);
            }

            if (!string.IsNullOrWhiteSpace(
                    search))
            {
                query =
                    query.Where(
                        item =>
                            item.Name.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase) ||
                            item.Slug.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase));
            }

            products =
                query
                    .OrderByDescending(
                        item =>
                            item.CreatedAtUtc)
                    .ThenBy(
                        item =>
                            item.Name)
                    .ToArray();
        }

        var totalCount =
            products.Length;

        var pageItems =
            products
                .Skip(
                    (page - 1) *
                    pageSize)
                .Take(
                    pageSize)
                .ToArray();

        var results =
            pageItems
                .Select(
                    product =>
                        new StorefrontProductSummaryResult(
                            product.Id.Value,
                            product.Name,
                            product.Slug,
                            product.Description,
                            product.CategoryId?.Value,
                            product.Price.Amount,
                            product.Price.Currency.Value,
                            product.CompareAtPrice?.Amount,
                            HasAvailableVariant(
                                tenantId,
                                product.Id)))
                .ToArray();

        var totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)pageSize);

        return Task.FromResult(
            new StorefrontProductPageResult(
                page,
                pageSize,
                totalCount,
                totalPages,
                results));
    }

    public Task<StorefrontProductDetailResult?> GetProductBySlugAsync(
        TenantId tenantId,
        string productSlug,
        CancellationToken cancellationToken = default)
    {
        Product? product;

        lock (_productStore.SyncRoot)
        {
            product =
                _productStore.Items
                    .SingleOrDefault(
                        item =>
                            item.TenantId ==
                                tenantId &&
                            !item.IsDeleted &&
                            item.Status ==
                                ProductStatus.Published &&
                            item.IsVisible &&
                            item.Slug ==
                                productSlug);
        }

        if (product is null)
        {
            return Task.FromResult<
                StorefrontProductDetailResult?>(
                    null);
        }

        Category? category =
            null;

        if (product.CategoryId.HasValue)
        {
            lock (_categoryStore.SyncRoot)
            {
                var categoryId =
                    product.CategoryId.Value;

                category =
                    _categoryStore.Items
                        .SingleOrDefault(
                            item =>
                                item.TenantId ==
                                    tenantId &&
                                item.Id ==
                                    categoryId &&
                                !item.IsDeleted &&
                                item.IsVisible);
            }
        }

        ProductVariant[] variants;

        lock (_variantStore.SyncRoot)
        {
            variants =
                _variantStore.Items
                    .Where(
                        item =>
                            item.TenantId ==
                                tenantId &&
                            item.ProductId ==
                                product.Id &&
                            !item.IsDeleted &&
                            item.IsEnabled)
                    .OrderByDescending(
                        item =>
                            item.IsDefault)
                    .ThenBy(
                        item =>
                            item.Name)
                    .ToArray();
        }

        var variantResults =
            variants
                .Select(
                    variant =>
                    {
                        var effectivePrice =
                            variant.PriceOverride
                            ?? product.Price;

                        return new StorefrontVariantResult(
                            variant.Id.Value,
                            variant.Name,
                            variant.Sku.Value,
                            variant.IsDefault,
                            effectivePrice.Amount,
                            effectivePrice.Currency.Value,
                            variant.Inventory.TrackInventory,
                            variant.Inventory.TrackInventory
                                ? variant.Inventory.Quantity
                                : null,
                            variant.Inventory
                                .ContinueSellingWhenOutOfStock,
                            variant.Inventory
                                .IsAvailableForSale);
                    })
                .ToArray();

        var attributes =
            LoadAttributes(
                tenantId,
                product.Id);

        return Task.FromResult<
            StorefrontProductDetailResult?>(
                new StorefrontProductDetailResult(
                    product.Id.Value,
                    product.Name,
                    product.Slug,
                    product.Description,
                    product.CategoryId?.Value,
                    category?.Name,
                    category?.Slug,
                    product.Price.Amount,
                    product.Price.Currency.Value,
                    product.CompareAtPrice?.Amount,
                    variantResults.Any(
                        item =>
                            item.AvailableForSale),
                    variantResults,
                    attributes));
    }

    private bool HasAvailableVariant(
        TenantId tenantId,
        ProductId productId)
    {
        lock (_variantStore.SyncRoot)
        {
            return _variantStore.Items.Any(
                item =>
                    item.TenantId ==
                        tenantId &&
                    item.ProductId ==
                        productId &&
                    !item.IsDeleted &&
                    item.IsEnabled &&
                    item.Inventory
                        .IsAvailableForSale);
        }
    }

    private IReadOnlyList<StorefrontProductAttributeResult>
        LoadAttributes(
            TenantId tenantId,
            ProductId productId)
    {
        TenantCommerceVertical? vertical;

        lock (_verticalStore.SyncRoot)
        {
            vertical =
                _verticalStore.Items
                    .SingleOrDefault(
                        item =>
                            item.TenantId ==
                                tenantId &&
                            item.IsEnabled &&
                            item.IsPrimary);
        }

        if (vertical is null)
        {
            return Array.Empty<
                StorefrontProductAttributeResult>();
        }

        var schema =
            ProductAttributeSchemaCatalog.Get(
                vertical.VerticalType);

        ProductAttributeValue[] values;

        lock (_attributeStore.SyncRoot)
        {
            values =
                _attributeStore.Items
                    .Where(
                        item =>
                            item.TenantId ==
                                tenantId &&
                            item.ProductId ==
                                productId)
                    .OrderBy(
                        item =>
                            item.Key)
                    .ToArray();
        }

        return values
            .Select(
                value =>
                    new
                    {
                        Value =
                            value,

                        Definition =
                            schema.Find(
                                value.Key)
                    })
            .Where(
                item =>
                    item.Definition is not null)
            .Select(
                item =>
                    new StorefrontProductAttributeResult(
                        item.Value.Key,
                        item.Definition!.Label,
                        item.Definition.ValueType.ToString(),
                        item.Value.Value))
            .ToArray();
    }

    private static StorefrontProductPageResult EmptyPage(
        int page,
        int pageSize)
    {
        return new StorefrontProductPageResult(
            page,
            pageSize,
            0,
            0,
            Array.Empty<
                StorefrontProductSummaryResult>());
    }
}