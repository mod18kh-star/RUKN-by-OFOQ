namespace OFOQ.Market.Contracts.Catalog;

public sealed record UpdateCategoryRequest(
    string Name,
    string Slug,
    bool IsVisible,
    string? ImageUrl = null);
