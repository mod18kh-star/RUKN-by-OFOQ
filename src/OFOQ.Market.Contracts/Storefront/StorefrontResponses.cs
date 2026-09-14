namespace OFOQ.Market.Contracts.Storefront;

public sealed record StorefrontInfoResponse(
    string Name,
    string Slug,
    string? Vertical,
    string? VerticalCode);

public sealed record StorefrontCategoryResponse(
    Guid CategoryId,
    string Name,
    string Slug,
    Guid? ParentCategoryId,
    int SortOrder);

public sealed record StorefrontProductSummaryResponse(
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

public sealed record StorefrontProductPageResponse(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<StorefrontProductSummaryResponse> Items);

public sealed record StorefrontVariantResponse(
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

public sealed record StorefrontProductAttributeResponse(
    string Key,
    string Label,
    string ValueType,
    string Value);

public sealed record StorefrontProductImageResponse(
    Guid ImageId,
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary);

public sealed record StorefrontProductDetailResponse(
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
    IReadOnlyList<StorefrontProductImageResponse> Images,
    IReadOnlyList<StorefrontVariantResponse> Variants,
    IReadOnlyList<StorefrontProductAttributeResponse> Attributes);