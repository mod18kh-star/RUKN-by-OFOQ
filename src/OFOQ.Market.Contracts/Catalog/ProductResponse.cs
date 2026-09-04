namespace OFOQ.Market.Contracts.Catalog;

public sealed record ProductResponse(
    Guid ProductId,
    string Name,
    string Slug,
    string? Description,
    Guid? CategoryId,
    decimal Price,
    string Currency,
    decimal? CompareAtPrice,
    string Status,
    bool IsVisible,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ProductVariantResponse> Variants);