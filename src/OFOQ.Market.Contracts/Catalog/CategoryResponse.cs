namespace OFOQ.Market.Contracts.Catalog;

public sealed record CategoryResponse(
    Guid CategoryId,
    string Name,
    string Slug,
    Guid? ParentCategoryId,
    int SortOrder,
    bool IsVisible,
    DateTimeOffset CreatedAtUtc);