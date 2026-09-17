using OFOQ.Market.Domain.Content;
namespace OFOQ.Market.Application.Common.Persistence;
public sealed record StorefrontContentPageResult(Guid Id,string Title,string Slug,string Body,string? SeoTitle,string? SeoDescription,DateTimeOffset? PublishedAtUtc);
public sealed record StorefrontNavigationItemResult(Guid Id,string Location,string Type,string Label,Guid? TargetId,string? ExternalUrl,Guid? ParentItemId,int SortOrder);
public interface IStorefrontContentQueryRepository
{
    Task<StorefrontContentPageResult?> GetPublishedPageAsync(string storeSlug,string pageSlug,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<StorefrontNavigationItemResult>?> GetNavigationAsync(string storeSlug,NavigationLocation location,CancellationToken cancellationToken=default);
}
