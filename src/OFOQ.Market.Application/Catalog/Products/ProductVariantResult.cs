using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products;

public sealed record ProductVariantResult(
    ProductVariantId VariantId,
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
    bool IsEnabled);