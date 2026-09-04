using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.Products.Variants.CreateVariant;

public sealed record CreateProductVariantCommand(
    ProductId ProductId,
    string Name,
    string Sku,
    decimal? PriceOverride,
    bool TrackInventory,
    int Quantity,
    int LowStockThreshold,
    bool ContinueSellingWhenOutOfStock,
    IReadOnlyList<ProductOptionValueId> OptionValueIds,
    UserId ActorUserId);