using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Categories;

public sealed record CategoryResult(
    CategoryId CategoryId,
    string Name,
    string Slug,
    CategoryId? ParentCategoryId,
    int SortOrder,
    bool IsVisible,
    DateTimeOffset CreatedAtUtc,
    string? ImageUrl = null);
