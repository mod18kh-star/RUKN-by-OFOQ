using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;

namespace OFOQ.Market.Application.Commerce.Carts;

public sealed record CartItemResult(
    CartItemId Id,
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal);