using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Storefront;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Catalog.Attributes;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class StorefrontQueryRepository :
    IStorefrontQueryRepository
{
    private readonly MarketDbContext
        _dbContext;

    public StorefrontQueryRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<StorefrontInfoResult?> GetStoreAsync(
        TenantSlug storeSlug,
        CancellationToken cancellationToken = default)
    {
        var tenant =
            await _dbContext
                .Tenants
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Slug ==
                            storeSlug &&
                        !item.IsDeleted &&
                        item.Status ==
                            TenantStatus.Active,
                    cancellationToken);

        if (tenant is null)
        {
            return null;
        }

        var primaryVertical =
            await _dbContext
                .TenantCommerceVerticals
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    vertical =>
                        vertical.TenantId ==
                            tenant.Id &&
                        vertical.IsEnabled &&
                        vertical.IsPrimary)
                .Select(
                    vertical =>
                        (CommerceVerticalType?)
                        vertical.VerticalType)
                .SingleOrDefaultAsync(
                    cancellationToken);

        string? verticalName =
            null;

        string? verticalCode =
            null;

        if (primaryVertical.HasValue)
        {
            var definition =
                CommerceVerticalCatalog.Get(
                    primaryVertical.Value);

            verticalName =
                primaryVertical.Value.ToString();

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

    public async Task<IReadOnlyList<StorefrontCategoryResult>>
        GetCategoriesAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
    {
        var categories =
            await _dbContext
                .Categories
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    category =>
                        category.TenantId ==
                            tenantId &&
                        !category.IsDeleted &&
                        category.IsVisible)
                .OrderBy(
                    category =>
                        category.SortOrder)
                .ThenBy(
                    category =>
                        category.Name)
                .ToArrayAsync(
                    cancellationToken);

        return categories
            .Select(
                category =>
                    new StorefrontCategoryResult(
                        category.Id.Value,
                        category.Name,
                        category.Slug,
                        category.ParentCategoryId?.Value,
                        category.SortOrder))
            .ToArray();
    }

    public async Task<StorefrontProductPageResult>
        GetProductsAsync(
            TenantId tenantId,
            string? search,
            string? categorySlug,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        var query =
            _dbContext
                .Products
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    product =>
                        product.TenantId ==
                            tenantId &&
                        !product.IsDeleted &&
                        product.Status ==
                            ProductStatus.Published &&
                        product.IsVisible);

        if (!string.IsNullOrWhiteSpace(
                categorySlug))
        {
            var category =
                await _dbContext
                    .Categories
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item =>
                            item.TenantId ==
                                tenantId &&
                            !item.IsDeleted &&
                            item.IsVisible &&
                            item.Slug ==
                                categorySlug,
                        cancellationToken);

            if (category is null)
            {
                return EmptyPage(
                    page,
                    pageSize);
            }

            query =
                query.Where(
                    product =>
                        product.CategoryId ==
                        category.Id);
        }

        if (!string.IsNullOrWhiteSpace(
                search))
        {
            var pattern =
                $"%{search}%";

            query =
                query.Where(
                    product =>
                        EF.Functions.ILike(
                            product.Name,
                            pattern) ||
                        EF.Functions.ILike(
                            product.Slug,
                            pattern));
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var products =
            await query
                .OrderByDescending(
                    product =>
                        product.CreatedAtUtc)
                .ThenBy(
                    product =>
                        product.Name)
                .Skip(
                    (page - 1) *
                    pageSize)
                .Take(
                    pageSize)
                .ToArrayAsync(
                    cancellationToken);

        var variantsByProduct =
            await LoadVariantsAsync(
                tenantId,
                products
                    .Select(
                        product =>
                            product.Id)
                    .ToArray(),
                cancellationToken);

        var items =
            products
                .Select(
                    product =>
                    {
                        variantsByProduct.TryGetValue(
                            product.Id,
                            out var variants);

                        var available =
                            variants?.Any(
                                variant =>
                                    variant.Inventory
                                        .IsAvailableForSale)
                            == true;

                        return new StorefrontProductSummaryResult(
                            product.Id.Value,
                            product.Name,
                            product.Slug,
                            product.Description,
                            product.CategoryId?.Value,
                            product.Price.Amount,
                            product.Price.Currency.Value,
                            product.CompareAtPrice?.Amount,
                            available);
                    })
                .ToArray();

        var totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)pageSize);

        return new StorefrontProductPageResult(
            page,
            pageSize,
            totalCount,
            totalPages,
            items);
    }

    public async Task<StorefrontProductDetailResult?>
        GetProductBySlugAsync(
            TenantId tenantId,
            string productSlug,
            CancellationToken cancellationToken = default)
    {
        var product =
            await _dbContext
                .Products
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.TenantId ==
                            tenantId &&
                        !item.IsDeleted &&
                        item.Status ==
                            ProductStatus.Published &&
                        item.IsVisible &&
                        item.Slug ==
                            productSlug,
                    cancellationToken);

        if (product is null)
        {
            return null;
        }

        Category? category =
            null;

        if (product.CategoryId.HasValue)
        {
            var categoryId =
                product.CategoryId.Value;

            category =
                await _dbContext
                    .Categories
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item =>
                            item.TenantId ==
                                tenantId &&
                            item.Id ==
                                categoryId &&
                            !item.IsDeleted &&
                            item.IsVisible,
                        cancellationToken);
        }

        var variants =
            await _dbContext
                .ProductVariants
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    variant =>
                        variant.TenantId ==
                            tenantId &&
                        variant.ProductId ==
                            product.Id &&
                        !variant.IsDeleted &&
                        variant.IsEnabled)
                .OrderByDescending(
                    variant =>
                        variant.IsDefault)
                .ThenBy(
                    variant =>
                        variant.Name)
                .ToArrayAsync(
                    cancellationToken);

        var attributes =
            await LoadAttributesAsync(
                tenantId,
                product.Id,
                cancellationToken);

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

        return new StorefrontProductDetailResult(
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
                variant =>
                    variant.AvailableForSale),
            variantResults,
            attributes);
    }

    private async Task<
        IReadOnlyDictionary<ProductId, ProductVariant[]>>
        LoadVariantsAsync(
            TenantId tenantId,
            IReadOnlyCollection<ProductId> productIds,
            CancellationToken cancellationToken)
    {
        if (productIds.Count ==
            0)
        {
            return new Dictionary<
                ProductId,
                ProductVariant[]>();
        }

        var variants =
            await _dbContext
                .ProductVariants
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    variant =>
                        variant.TenantId ==
                            tenantId &&
                        productIds.Contains(
                            variant.ProductId) &&
                        !variant.IsDeleted &&
                        variant.IsEnabled)
                .ToArrayAsync(
                    cancellationToken);

        return variants
            .GroupBy(
                variant =>
                    variant.ProductId)
            .ToDictionary(
                group =>
                    group.Key,
                group =>
                    group.ToArray());
    }

    private async Task<
        IReadOnlyList<StorefrontProductAttributeResult>>
        LoadAttributesAsync(
            TenantId tenantId,
            ProductId productId,
            CancellationToken cancellationToken)
    {
        var verticalType =
            await _dbContext
                .TenantCommerceVerticals
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    vertical =>
                        vertical.TenantId ==
                            tenantId &&
                        vertical.IsEnabled &&
                        vertical.IsPrimary)
                .Select(
                    vertical =>
                        (CommerceVerticalType?)
                        vertical.VerticalType)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (!verticalType.HasValue)
        {
            return Array.Empty<
                StorefrontProductAttributeResult>();
        }

        var schema =
            ProductAttributeSchemaCatalog.Get(
                verticalType.Value);

        var values =
            await _dbContext
                .ProductAttributeValues
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    value =>
                        value.TenantId ==
                            tenantId &&
                        value.ProductId ==
                            productId)
                .OrderBy(
                    value =>
                        value.Key)
                .ToArrayAsync(
                    cancellationToken);

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