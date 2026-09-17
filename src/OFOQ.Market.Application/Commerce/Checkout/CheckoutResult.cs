using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed record CheckoutResult(
    OrderId OrderId,
    CartId SourceCartId,
    string Status,
    string Currency,
    int TotalQuantity,
    decimal SubtotalAmount,
    decimal ShippingAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    string? AppliedCouponCode,
    string? ShippingMethodName,
    DateTimeOffset CreatedAtUtc,
    bool IsIdempotentReplay,
    IReadOnlyList<CheckoutItemResult> Items);
