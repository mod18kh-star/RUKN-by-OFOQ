using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Carts.AddItem;

public sealed record AddToCartCommand(
    UserId CustomerUserId,
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    int Quantity);