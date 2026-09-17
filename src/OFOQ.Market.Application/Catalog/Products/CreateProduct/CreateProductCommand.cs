using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Slug,
    string? Description,
    CategoryId? CategoryId,
    decimal Price,
    decimal? CompareAtPrice,
    string Currency,
    string Sku,
    bool TrackInventory,
    int Quantity,
    int LowStockThreshold,
    bool ContinueSellingWhenOutOfStock,
    UserId ActorUserId,
    string? PrimaryImageUrl = null);
