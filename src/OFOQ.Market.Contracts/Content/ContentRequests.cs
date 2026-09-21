namespace OFOQ.Market.Contracts.Content;

public sealed record UpsertContentPageRequest(
    string Title,
    string Slug,
    string Body,
    string? SeoTitle,
    string? SeoDescription,
    bool Publish,
    string PageKind = "Standard",
    string? HeroImageUrl = null,
    bool ShowCustomerCount = true,
    bool ShowCompletedOrderCount = true,
    bool ShowUnitsSold = true,
    bool ShowAverageRating = true,
    bool ShowReviewCount = true,
    bool ShowCountryCount = true);

public sealed record ContentPageResponse(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? SeoTitle,
    string? SeoDescription,
    bool IsPublished,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset CreatedAtUtc,
    string PageKind = "Standard",
    string? HeroImageUrl = null,
    bool ShowCustomerCount = true,
    bool ShowCompletedOrderCount = true,
    bool ShowUnitsSold = true,
    bool ShowAverageRating = true,
    bool ShowReviewCount = true,
    bool ShowCountryCount = true);

public sealed record UpsertNavigationItemRequest(
    string Location,
    string Type,
    string Label,
    Guid? TargetId,
    string? ExternalUrl,
    Guid? ParentItemId,
    int? Position,
    bool IsVisible);

public sealed record NavigationItemResponse(
    Guid Id,
    string Location,
    string Type,
    string Label,
    Guid? TargetId,
    string? ExternalUrl,
    Guid? ParentItemId,
    int SortOrder,
    bool IsVisible);

public sealed record StorefrontPageResponse(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? SeoTitle,
    string? SeoDescription,
    DateTimeOffset? PublishedAtUtc,
    string PageKind = "Standard",
    string? HeroImageUrl = null);

public sealed record StorefrontPageStatisticsResponse(
    long? CustomerCount,
    long? CompletedOrderCount,
    long? UnitsSold,
    decimal? AverageRating,
    int? ReviewCount,
    long? CountryCount);

public sealed record StorefrontNavigationResponse(
    Guid Id,
    string Location,
    string Type,
    string Label,
    Guid? TargetId,
    string? ExternalUrl,
    Guid? ParentItemId,
    int SortOrder,
    string Href);
