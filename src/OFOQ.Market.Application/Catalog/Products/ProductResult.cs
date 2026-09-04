using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products;

public sealed record ProductResult(
    ProductId ProductId,
    string Name,
    string Slug,
    string? Description,
    CategoryId? CategoryId,
    decimal Price,
    string Currency,
    decimal? CompareAtPrice,
    ProductStatus Status,
    bool IsVisible,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ProductVariantResult> Variants);