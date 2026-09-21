namespace OFOQ.Market.Contracts.Catalog;

public sealed record CreateProductRequest(
    string Name,
    string Slug,
    string? Description,
    Guid? CategoryId,
    decimal Price,
    decimal? CompareAtPrice,
    string Currency,
    string Sku,
    bool TrackInventory,
    int Quantity,
    int LowStockThreshold,
    bool ContinueSellingWhenOutOfStock,
    string? PrimaryImageUrl = null,
    string? VerticalCode = null);
