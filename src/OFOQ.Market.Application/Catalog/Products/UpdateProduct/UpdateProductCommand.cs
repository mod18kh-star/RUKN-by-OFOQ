using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    ProductId ProductId,
    string Name,
    string Slug,
    string? Description,
    CategoryId? CategoryId,
    decimal Price,
    decimal? CompareAtPrice,
    string Currency,
    string Sku,
    UserId ActorUserId);