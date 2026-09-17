namespace OFOQ.Market.Contracts.Catalog;

public sealed record CreateCategoryRequest(
    string Name,
    string Slug,
    Guid? ParentCategoryId = null,
    int? Position = null,
    int? SortOrder = null,
    string? ImageUrl = null);
