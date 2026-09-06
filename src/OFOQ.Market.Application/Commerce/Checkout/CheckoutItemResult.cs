using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed record CheckoutItemResult(
    OrderItemId Id,
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    string ProductName,
    string VariantName,
    string Sku,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal);
