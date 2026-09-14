using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Application.Commerce.Orders.Cancel;

public sealed record CancelOrderInventoryResult(
    ProductVariantId ProductVariantId,
    int RestoredQuantity,
    int QuantityAfter);

public sealed record CancelOrderResult(
    OrderId OrderId,
    OrderStatus Status,
    OrderFulfillmentStatus FulfillmentStatus,
    string? CancellationReason,
    DateTimeOffset? CancelledAtUtc,
    bool IsIdempotentReplay,
    IReadOnlyList<CancelOrderInventoryResult> Inventory);