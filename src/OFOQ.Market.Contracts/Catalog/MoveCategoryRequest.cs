namespace OFOQ.Market.Contracts.Catalog;

public sealed record MoveCategoryRequest(
    Guid? ParentCategoryId = null,
    int? Position = null);
