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
                        item.Slug == storeSlug &&
                        !item.IsDeleted &&
                        item.Status == TenantStatus.Active,
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
                        vertical.TenantId == tenant.Id &&
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

        var presentation =
            await _dbContext
                .TenantStorefrontPresentations
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.TenantId == tenant.Id,
                    cancellationToken);

        var publicPresentation =
            presentation is null
                ? new StorefrontPresentationPublicResult(
                    null,
                    null,
                    null,
                    null,
                    null,
                    TenantStorefrontPresentation.DefaultThemePresetCode,
                    TenantStorefrontPresentation.DefaultFontCode,
                    true,
                    true,
                    TenantStorefrontPresentation.DefaultCategorySectionTitle,
                    TenantStorefrontPresentation.DefaultProductSectionTitle)
                : new StorefrontPresentationPublicResult(
                    presentation.LogoUrl,
                    presentation.CoverImageUrl,
                    presentation.Announcement,
                    presentation.PrimaryColor,
                    presentation.AccentColor,
                    presentation.ThemePresetCode,
                    presentation.FontCode,
                    presentation.ShowCategoriesOnHome,
                    presentation.ShowProductsOnHome,
                    presentation.CategorySectionTitle,
                    presentation.ProductSectionTitle,
                    presentation.VisualContentJson);

        var profile =
            await _dbContext
                .TenantStoreProfiles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.TenantId == tenant.Id,
                    cancellationToken);

        var socialLinks =
            await _dbContext
                .TenantStoreSocialLinks
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    item =>
                        item.TenantId == tenant.Id &&
                        item.IsVisible)
                .OrderBy(
                    item =>
                        item.SortOrder)
                .ThenBy(
                    item =>
                        item.PlatformCode)
                .Select(
                    item =>
                        new StorefrontSocialLinkPublicResult(
                            item.PlatformCode,
                            item.Label,
                            item.Url,
                            item.SortOrder))
                .ToArrayAsync(
                    cancellationToken);

        var publicContact =
            new StorefrontContactPublicResult(
                profile?.ShowWebsite == true
                    ? profile.WebsiteUrl
                    : null,
                profile?.ShowWhatsApp == true
                    ? profile.WhatsAppNumber
                    : null,
                profile?.ShowCustomerServicePhone == true
                    ? profile.CustomerServicePhone
                    : null,
                profile?.ShowSecondaryPhone == true
                    ? profile.SecondaryPhone
                    : null,
                profile?.ShowLandlinePhone == true
                    ? profile.LandlinePhone
                    : null,
                profile?.ShowPhysicalAddress == true
                    ? profile.PhysicalAddress
                    : null,
                profile?.ShowPhysicalAddress == true
                    ? profile.GoogleMapsUrl
                    : null,
                profile is not null &&
                profile.ShowCommercialRegistration &&
                !profile.CommercialRegistrationNotApplicable
                    ? profile.CommercialRegistrationNumber
                    : null,
                socialLinks);

        return new StorefrontInfoResult(
            tenant.Id,
            tenant.Name,
            tenant.Slug.Value,
            verticalName,
            verticalCode,
            publicPresentation,
            publicContact);
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
                        category.TenantId == tenantId &&
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
                        category.SortOrder,
                        category.ImageUrl))
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
                        product.TenantId == tenantId &&
                        !product.IsDeleted &&
                        product.Status == ProductStatus.Published &&
                        product.IsVisible)
                ;


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
                            item.TenantId == tenantId &&
                            !item.IsDeleted &&
                            item.IsVisible &&
                            item.Slug == categorySlug,
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

        var productIds =
            products
                .Select(
                    product =>
                        product.Id)
                .ToArray();

        var variantsByProduct =
            await LoadVariantsAsync(
                tenantId,
                productIds,
                cancellationToken);

        var primaryImages =
            await LoadPrimaryImagesAsync(
                tenantId,
                productIds,
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

                        primaryImages.TryGetValue(
                            product.Id,
                            out var primaryImage);

                        return new StorefrontProductSummaryResult(
                            product.Id.Value,
                            product.Name,
                            product.Slug,
                            product.Description,
                            product.CategoryId?.Value,
                            product.Price.Amount,
                            product.Price.Currency.Value,
                            product.CompareAtPrice?.Amount,
                            available,
                            primaryImage?.Url,
                            primaryImage?.AltText);
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
                        item.TenantId == tenantId &&
                        !item.IsDeleted &&
                        item.Status == ProductStatus.Published &&
                        item.IsVisible &&
                        item.Slug == productSlug,
                    cancellationToken);

        if (product is null)
        {
            return null;
        }

        var images =
            await _dbContext
                .ProductImages
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(
                    image =>
                        image.TenantId == tenantId &&
                        image.ProductId == product.Id)
                .OrderBy(
                    image =>
                        image.SortOrder)
                .ThenBy(
                    image =>
                        image.Id)
                .ToArrayAsync(
                    cancellationToken);

        var primaryImage =
            images.FirstOrDefault(
                image =>
                    image.IsPrimary);

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
                            item.TenantId == tenantId &&
                            item.Id == categoryId &&
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
                        variant.TenantId == tenantId &&
                        variant.ProductId == product.Id &&
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

        var imageResults =
            images
                .Select(
                    image =>
                        new StorefrontProductImageResult(
                            image.Id.Value,
                            image.Url,
                            image.AltText,
                            image.SortOrder,
                            image.IsPrimary))
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
            primaryImage?.Url,
            primaryImage?.AltText,
            imageResults,
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
        if (productIds.Count == 0)
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
                        variant.TenantId == tenantId &&
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
        IReadOnlyDictionary<ProductId, ProductImage>>
        LoadPrimaryImagesAsync(
            TenantId tenantId,
            IReadOnlyCollection<ProductId> productIds,
            CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<
                ProductId,
                ProductImage>();
        }

        return await _dbContext
            .ProductImages
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(
                image =>
                    image.TenantId == tenantId &&
                    productIds.Contains(
                        image.ProductId) &&
                    image.IsPrimary)
            .ToDictionaryAsync(
                image =>
                    image.ProductId,
                cancellationToken);
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
                        vertical.TenantId == tenantId &&
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
                        value.TenantId == tenantId &&
                        value.ProductId == productId)
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

    public async Task<IReadOnlyList<CartProductDisplayResult>>
        GetCartProductsAsync(
            TenantId tenantId,
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken = default)
    {
        if (tenantId.IsEmpty || productIds.Count == 0)
        {
            return Array.Empty<CartProductDisplayResult>();
        }

        var ids = productIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Select(ProductId.From)
            .ToArray();

        if (ids.Length == 0)
        {
            return Array.Empty<CartProductDisplayResult>();
        }

        var products = await _dbContext.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(product =>
                product.TenantId == tenantId &&
                ids.Contains(product.Id) &&
                !product.IsDeleted &&
                product.Status == ProductStatus.Published &&
                product.IsVisible)
            .ToArrayAsync(cancellationToken);

        if (products.Length == 0)
        {
            return Array.Empty<CartProductDisplayResult>();
        }

        var validIds = products
            .Select(product => product.Id)
            .ToArray();

        var images = await _dbContext.ProductImages
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(image =>
                image.TenantId == tenantId &&
                validIds.Contains(image.ProductId))
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.SortOrder)
            .ToArrayAsync(cancellationToken);

        var variants = await _dbContext.ProductVariants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(variant =>
                variant.TenantId == tenantId &&
                validIds.Contains(variant.ProductId) &&
                !variant.IsDeleted &&
                variant.IsEnabled)
            .ToArrayAsync(cancellationToken);

        var imagesByProduct = images
            .GroupBy(image => image.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        var variantsByProduct = variants
            .GroupBy(variant => variant.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(variant =>
                        new CartVariantDisplayResult(
                            variant.Id.Value,
                            variant.Name))
                    .ToArray());

        return products
            .Select(product =>
            {
                imagesByProduct.TryGetValue(
                    product.Id,
                    out var image);

                variantsByProduct.TryGetValue(
                    product.Id,
                    out var productVariants);

                return new CartProductDisplayResult(
                    product.Id.Value,
                    product.Name,
                    product.Slug,
                    product.Price.Currency.Value,
                    product.Price.Amount,
                    product.CompareAtPrice?.Amount,
                    image?.Url,
                    image?.AltText,
                    productVariants ??
                        Array.Empty<CartVariantDisplayResult>());
            })
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