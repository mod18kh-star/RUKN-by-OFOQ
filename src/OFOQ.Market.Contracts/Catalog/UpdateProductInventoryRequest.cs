namespace OFOQ.Market.Contracts.Catalog;

public sealed record UpdateProductInventoryRequest(
    bool TrackInventory,
    int Quantity,
    int LowStockThreshold,
    bool ContinueSellingWhenOutOfStock);