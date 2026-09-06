namespace OFOQ.Market.Contracts.Commerce.Checkout;

public sealed record CheckoutResponse(
    Guid OrderId,
    Guid SourceCartId,
    string Status,
    string Currency,
    int TotalQuantity,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc,
    bool IdempotentReplay,
    IReadOnlyList<CheckoutItemResponse> Items);
