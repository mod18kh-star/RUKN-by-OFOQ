using OFOQ.Market.Domain.Content;

namespace OFOQ.Market.Application.Common.Persistence;

public sealed record StorefrontContentPageResult(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? SeoTitle,
    string? SeoDescription,
    DateTimeOffset? PublishedAtUtc,
    string PageKind = "Standard",
    string? HeroImageUrl = null);

public sealed record StorefrontPageStatisticsResult(
    long? CustomerCount,
    long? CompletedOrderCount,
    long? UnitsSold,
    decimal? AverageRating,
    int? ReviewCount,
    long? CountryCount);

public sealed record StorefrontNavigationItemResult(
    Guid Id,
    string Location,
    string Type,
    string Label,
    Guid? TargetId,
    string? ExternalUrl,
    Guid? ParentItemId,
    int SortOrder,
    string Href);

public interface IStorefrontContentQueryRepository
{
    Task<StorefrontContentPageResult?> GetPublishedPageAsync(
        string storeSlug,
        string pageSlug,
        CancellationToken cancellationToken = default);

    Task<StorefrontPageStatisticsResult?> GetPublishedPageStatisticsAsync(
        string storeSlug,
        string pageSlug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StorefrontNavigationItemResult>?> GetNavigationAsync(
        string storeSlug,
        NavigationLocation location,
        CancellationToken cancellationToken = default);
}
