namespace OFOQ.Market.Contracts.Commerce.Orders;

public sealed record MerchantOrderItemResponse(
    Guid OrderItemId,
    Guid ProductId,
    Guid ProductVariantId,
    string ProductName,
    string VariantName,
    string Sku,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal);

public sealed record MerchantOrderTimelineResponse(
    string Type,
    string OrderStatus,
    string FulfillmentStatus,
    string? Note,
    DateTimeOffset CreatedAtUtc);

public sealed record MerchantOrderSummaryResponse(
    Guid OrderId,
    Guid CustomerUserId,
    string? CustomerEmail,
    string OrderStatus,
    string PaymentStatus,
    string FulfillmentStatus,
    string Currency,
    int TotalQuantity,
    decimal TotalAmount,
    string? ShippingCarrier,
    string? TrackingNumber,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record MerchantOrderDetailResponse(
    Guid OrderId,
    Guid CustomerUserId,
    string? CustomerEmail,
    string OrderStatus,
    string PaymentStatus,
    string FulfillmentStatus,
    string Currency,
    int TotalQuantity,
    decimal TotalAmount,
    string? ShippingCarrier,
    string? TrackingNumber,
    string? CancellationReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? ShippedAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    DateTimeOffset? CancelledAtUtc,
    IReadOnlyList<MerchantOrderItemResponse> Items,
    IReadOnlyList<MerchantOrderTimelineResponse> Timeline);

public sealed record OrderLifecycleResponse(
    Guid OrderId,
    string OrderStatus,
    string FulfillmentStatus,
    string? ShippingCarrier,
    string? TrackingNumber,
    DateTimeOffset? UpdatedAtUtc,
    bool IsIdempotentReplay);

public sealed record OrderInventoryRestockResponse(
    Guid ProductVariantId,
    int RestoredQuantity,
    int QuantityAfter);

public sealed record CancelOrderResponse(
    Guid OrderId,
    string OrderStatus,
    string FulfillmentStatus,
    string? CancellationReason,
    DateTimeOffset? CancelledAtUtc,
    bool IsIdempotentReplay,
    IReadOnlyList<OrderInventoryRestockResponse> Inventory);