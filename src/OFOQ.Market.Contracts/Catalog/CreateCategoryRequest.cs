namespace OFOQ.Market.Contracts.Catalog;

public sealed record CreateCategoryRequest(
    string Name,
    string Slug,
    Guid? ParentCategoryId = null,
    int SortOrder = 0);