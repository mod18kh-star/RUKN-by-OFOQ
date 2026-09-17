using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Storefront;

public sealed record StorefrontPresentationPublicResult(
    string? LogoUrl,
    string? CoverImageUrl,
    string? Announcement,
    string? PrimaryColor,
    string? AccentColor,
    string ThemePresetCode,
    string FontCode,
    bool ShowCategoriesOnHome,
    bool ShowProductsOnHome,
    string CategorySectionTitle,
    string ProductSectionTitle);

public sealed record StorefrontInfoResult(
    TenantId TenantId,
    string Name,
    string Slug,
    string? Vertical,
    string? VerticalCode,
    StorefrontPresentationPublicResult Presentation);

public sealed record StorefrontCategoryResult(
    Guid CategoryId,
    string Name,
    string Slug,
    Guid? ParentCategoryId,
    int SortOrder,
    string? ImageUrl = null);

public sealed record StorefrontProductSummaryResult(
    Guid ProductId,
    string Name,
    string Slug,
    string? Description,
    Guid? CategoryId,
    decimal Price,
    string Currency,
    decimal? CompareAtPrice,
    bool AvailableForSale,
    string PrimaryImageUrl,
    string? PrimaryImageAltText);

public sealed record StorefrontProductPageResult(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<StorefrontProductSummaryResult> Items);

public sealed record StorefrontVariantResult(
    Guid VariantId,
    string Name,
    string Sku,
    bool IsDefault,
    decimal Price,
    string Currency,
    bool TrackInventory,
    int? Quantity,
    bool ContinueSellingWhenOutOfStock,
    bool AvailableForSale);

public sealed record StorefrontProductAttributeResult(
    string Key,
    string Label,
    string ValueType,
    string Value);

public sealed record StorefrontProductImageResult(
    Guid ImageId,
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary);

public sealed record StorefrontProductDetailResult(
    Guid ProductId,
    string Name,
    string Slug,
    string? Description,
    Guid? CategoryId,
    string? CategoryName,
    string? CategorySlug,
    decimal Price,
    string Currency,
    decimal? CompareAtPrice,
    bool AvailableForSale,
    string PrimaryImageUrl,
    string? PrimaryImageAltText,
    IReadOnlyList<StorefrontProductImageResult> Images,
    IReadOnlyList<StorefrontVariantResult> Variants,
    IReadOnlyList<StorefrontProductAttributeResult> Attributes);

public sealed record StorefrontProductLookupResult(
    bool StoreExists,
    StorefrontProductDetailResult? Product);