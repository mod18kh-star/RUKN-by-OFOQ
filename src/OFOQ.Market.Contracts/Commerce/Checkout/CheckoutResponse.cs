namespace OFOQ.Market.Contracts.Commerce.Checkout;

public sealed record CheckoutResponse(
    Guid OrderId,
    Guid SourceCartId,
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
    bool IdempotentReplay,
    IReadOnlyList<CheckoutItemResponse> Items);
