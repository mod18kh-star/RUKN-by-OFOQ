namespace OFOQ.Market.Contracts.Catalog;

public sealed record CreateStructuredProductVariantRequest(
    string Name,
    string Sku,
    decimal? PriceOverride,
    bool TrackInventory,
    int Quantity,
    int LowStockThreshold,
    bool ContinueSellingWhenOutOfStock,
    IReadOnlyList<Guid> OptionValueIds);