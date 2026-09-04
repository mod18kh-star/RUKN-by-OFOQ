namespace OFOQ.Market.Contracts.Catalog;

public sealed record StructuredProductVariantResponse(
    Guid VariantId,
    string Name,
    string Sku,
    bool IsDefault,
    decimal? PriceOverride,
    string? PriceOverrideCurrency,
    bool TrackInventory,
    int Quantity,
    int LowStockThreshold,
    bool ContinueSellingWhenOutOfStock,
    bool IsAvailableForSale,
    bool IsEnabled,
    IReadOnlyList<ProductVariantSelectionResponse> Selections);