namespace OFOQ.Market.Contracts.Catalog;

public sealed record UpdateProductRequest(
    string Name,
    string Slug,
    string? Description,
    Guid? CategoryId,
    decimal Price,
    decimal? CompareAtPrice,
    string Currency,
    string Sku);