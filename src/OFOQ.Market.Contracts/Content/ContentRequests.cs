namespace OFOQ.Market.Contracts.Content;

public sealed record UpsertContentPageRequest(string Title,string Slug,string Body,string? SeoTitle,string? SeoDescription,bool Publish);
public sealed record ContentPageResponse(Guid Id,string Title,string Slug,string Body,string? SeoTitle,string? SeoDescription,bool IsPublished,DateTimeOffset? PublishedAtUtc,DateTimeOffset CreatedAtUtc);
public sealed record UpsertNavigationItemRequest(string Location,string Type,string Label,Guid? TargetId,string? ExternalUrl,Guid? ParentItemId,int? Position,bool IsVisible);
public sealed record NavigationItemResponse(Guid Id,string Location,string Type,string Label,Guid? TargetId,string? ExternalUrl,Guid? ParentItemId,int SortOrder,bool IsVisible);
public sealed record StorefrontPageResponse(Guid Id,string Title,string Slug,string Body,string? SeoTitle,string? SeoDescription,DateTimeOffset? PublishedAtUtc);
public sealed record StorefrontNavigationResponse(Guid Id,string Location,string Type,string Label,Guid? TargetId,string? ExternalUrl,Guid? ParentItemId,int SortOrder);
