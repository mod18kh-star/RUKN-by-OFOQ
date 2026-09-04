using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.Products.Inventory;

public sealed record UpdateProductInventoryCommand(
    ProductId ProductId,
    bool TrackInventory,
    int Quantity,
    int LowStockThreshold,
    bool ContinueSellingWhenOutOfStock,
    UserId ActorUserId);