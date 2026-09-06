using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed record CheckoutResult(
    OrderId OrderId,
    CartId SourceCartId,
    string Status,
    string Currency,
    int TotalQuantity,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc,
    bool IsIdempotentReplay,
    IReadOnlyList<CheckoutItemResult> Items);
