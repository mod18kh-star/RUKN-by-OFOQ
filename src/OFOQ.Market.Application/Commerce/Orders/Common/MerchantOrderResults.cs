using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Application.Commerce.Orders.Common;

public sealed record MerchantOrderItemResult(
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

public sealed record MerchantOrderTimelineResult(
    string Type,
    string OrderStatus,
    string FulfillmentStatus,
    string? Note,
    DateTimeOffset CreatedAtUtc);

public sealed record MerchantOrderSummaryResult(
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

public sealed record MerchantOrderDetailResult(
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
    IReadOnlyList<MerchantOrderItemResult> Items,
    IReadOnlyList<MerchantOrderTimelineResult> Timeline);

internal static class MerchantOrderResultMapper
{
    public static string MapPaymentStatus(
        OFOQ.Market.Domain.Commerce.Payments.Payment? payment)
    {
        return payment?.Status switch
        {
            OFOQ.Market.Domain.Commerce.Payments.PaymentStatus.Succeeded =>
                "Paid",

            OFOQ.Market.Domain.Commerce.Payments.PaymentStatus.Pending =>
                "Pending",

            OFOQ.Market.Domain.Commerce.Payments.PaymentStatus.Cancelled =>
                "Cancelled",

            null =>
                "NotStarted",

            _ =>
                payment.Status.ToString()
        };
    }

    public static MerchantOrderSummaryResult MapSummary(
        Order order,
        OFOQ.Market.Domain.Commerce.Payments.Payment? payment,
        string? customerEmail)
    {
        return new MerchantOrderSummaryResult(
            order.Id.Value,
            order.CustomerUserId.Value,
            customerEmail,
            order.Status.ToString(),
            MapPaymentStatus(
                payment),
            order.FulfillmentStatus.ToString(),
            order.Currency.Value,
            order.TotalQuantity,
            order.TotalAmount,
            order.ShippingCarrier,
            order.TrackingNumber,
            order.CreatedAtUtc,
            order.UpdatedAtUtc);
    }

    public static MerchantOrderDetailResult MapDetail(
        Order order,
        OFOQ.Market.Domain.Commerce.Payments.Payment? payment,
        string? customerEmail)
    {
        return new MerchantOrderDetailResult(
            order.Id.Value,
            order.CustomerUserId.Value,
            customerEmail,
            order.Status.ToString(),
            MapPaymentStatus(
                payment),
            order.FulfillmentStatus.ToString(),
            order.Currency.Value,
            order.TotalQuantity,
            order.TotalAmount,
            order.ShippingCarrier,
            order.TrackingNumber,
            order.CancellationReason,
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.ShippedAtUtc,
            order.DeliveredAtUtc,
            order.CancelledAtUtc,
            order.Items
                .Select(
                    item =>
                        new MerchantOrderItemResult(
                            item.Id.Value,
                            item.ProductId.Value,
                            item.ProductVariantId.Value,
                            item.ProductName,
                            item.VariantName,
                            item.Sku,
                            item.UnitPrice.Amount,
                            item.UnitPrice.Currency.Value,
                            item.Quantity,
                            item.LineTotal))
                .ToArray(),
            order.Timeline
                .OrderBy(
                    entry =>
                        entry.CreatedAtUtc)
                .Select(
                    entry =>
                        new MerchantOrderTimelineResult(
                            entry.Type.ToString(),
                            entry.OrderStatus.ToString(),
                            entry.FulfillmentStatus.ToString(),
                            entry.Note,
                            entry.CreatedAtUtc))
                .ToArray());
    }
}