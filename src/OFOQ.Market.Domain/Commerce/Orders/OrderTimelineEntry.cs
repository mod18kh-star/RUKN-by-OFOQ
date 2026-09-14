using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Orders;

public sealed class OrderTimelineEntry :
    Entity<OrderTimelineEntryId>,
    ITenantDataScoped
{
    private OrderTimelineEntry()
    {
    }

    private OrderTimelineEntry(
        OrderTimelineEntryId id,
        TenantId tenantId,
        OrderId orderId,
        OrderTimelineEntryType type,
        OrderStatus orderStatus,
        OrderFulfillmentStatus fulfillmentStatus,
        string? note,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId =
            tenantId;

        OrderId =
            orderId;

        Type =
            type;

        OrderStatus =
            orderStatus;

        FulfillmentStatus =
            fulfillmentStatus;

        Note =
            NormalizeOptional(
                note,
                500,
                nameof(note));

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public OrderId OrderId { get; private set; }

    public OrderTimelineEntryType Type { get; private set; }

    public OrderStatus OrderStatus { get; private set; }

    public OrderFulfillmentStatus FulfillmentStatus { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    internal static OrderTimelineEntry Create(
        TenantId tenantId,
        OrderId orderId,
        OrderTimelineEntryType type,
        OrderStatus orderStatus,
        OrderFulfillmentStatus fulfillmentStatus,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null,
        string? note = null)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (orderId.IsEmpty)
        {
            throw new ArgumentException(
                "Order ID cannot be empty.",
                nameof(orderId));
        }

        return new OrderTimelineEntry(
            OrderTimelineEntryId.New(),
            tenantId,
            orderId,
            type,
            orderStatus,
            fulfillmentStatus,
            note,
            createdAtUtc,
            createdByUserId);
    }

    private static string? NormalizeOptional(
        string? value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }

        var normalized =
            value.Trim();

        if (normalized.Length >
            maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}