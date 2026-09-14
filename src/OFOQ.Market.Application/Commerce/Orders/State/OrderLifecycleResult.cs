namespace OFOQ.Market.Application.Commerce.Orders.State;

public sealed record OrderLifecycleResult(
    Guid OrderId,
    string OrderStatus,
    string FulfillmentStatus,
    string? ShippingCarrier,
    string? TrackingNumber,
    DateTimeOffset? UpdatedAtUtc,
    bool IsIdempotentReplay);