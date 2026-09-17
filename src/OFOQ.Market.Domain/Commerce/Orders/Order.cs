using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Orders;

public sealed partial class Order :
    AggregateRoot<OrderId>,
    ITenantDataScoped,
    IAuditable
{
    private readonly List<OrderItem> _items =
        [];

    private readonly List<OrderTimelineEntry> _timeline =
        [];

    private Guid _customerUserId;

    private CartId _sourceCartId;

    private string _currencyCode =
        string.Empty;

    private Order()
    {
    }

    private Order(
        OrderId id,
        TenantId tenantId,
        UserId customerUserId,
        CartId sourceCartId,
        CurrencyCode currency,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (customerUserId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(customerUserId));
        }

        if (sourceCartId.IsEmpty)
        {
            throw new ArgumentException(
                "Source cart ID cannot be empty.",
                nameof(sourceCartId));
        }

        if (currency.IsEmpty)
        {
            throw new ArgumentException(
                "Order currency cannot be empty.",
                nameof(currency));
        }

        TenantId =
            tenantId;

        _customerUserId =
            customerUserId.Value;

        _sourceCartId =
            sourceCartId;

        _currencyCode =
            currency.Value;

        Status =
            OrderStatus.Pending;

        FulfillmentStatus =
            OrderFulfillmentStatus.Unfulfilled;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public UserId CustomerUserId =>
        UserId.From(
            _customerUserId);

    public CartId SourceCartId =>
        _sourceCartId;

    public CurrencyCode Currency =>
        CurrencyCode.Create(
            _currencyCode);

    public OrderStatus Status { get; private set; }

    public OrderFulfillmentStatus FulfillmentStatus { get; private set; }

    public string? ShippingCarrier { get; private set; }

    public string? TrackingNumber { get; private set; }

    public string? CancellationReason { get; private set; }

    public DateTimeOffset? ShippedAtUtc { get; private set; }

    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public IReadOnlyCollection<OrderItem> Items =>
        _items.AsReadOnly();

    public IReadOnlyCollection<OrderTimelineEntry> Timeline =>
        _timeline.AsReadOnly();

    public int TotalQuantity =>
        _items.Sum(
            item =>
                item.Quantity);

    public decimal SubtotalAmount =>
        _items.Sum(
            item =>
                item.LineTotal);

    public decimal TotalAmount =>
        Math.Max(
            0m,
            SubtotalAmount +
            ShippingAmount -
            DiscountAmount);

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static Order Create(
        TenantId tenantId,
        UserId customerUserId,
        CartId sourceCartId,
        CurrencyCode currency,
        IReadOnlyCollection<OrderItemSnapshot> items,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        ArgumentNullException.ThrowIfNull(
            items);

        if (items.Count == 0)
        {
            throw new ArgumentException(
                "An order must contain at least one item.",
                nameof(items));
        }

        var order =
            new Order(
                OrderId.New(),
                tenantId,
                customerUserId,
                sourceCartId,
                currency,
                createdAtUtc,
                createdByUserId);

        var seenVariantIds =
            new HashSet<ProductVariantId>();

        foreach (var snapshot in
                 items)
        {
            ArgumentNullException.ThrowIfNull(
                snapshot);

            if (snapshot.UnitPrice.Currency !=
                currency)
            {
                throw new ArgumentException(
                    "Every order item must use the order currency.",
                    nameof(items));
            }

            if (!seenVariantIds.Add(
                    snapshot.ProductVariantId))
            {
                throw new ArgumentException(
                    "An order cannot contain the same product variant more than once.",
                    nameof(items));
            }

            order._items.Add(
                OrderItem.Create(
                    tenantId,
                    order.Id,
                    snapshot));
        }

        order.RecordTimeline(
            OrderTimelineEntryType.Created,
            createdAtUtc,
            createdByUserId);

        return order;
    }

    public void MarkPaid(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status ==
            OrderStatus.Paid)
        {
            return;
        }

        EnsureOrderStatus(
            OrderStatus.Pending);

        SetOrderStatus(
            OrderStatus.Paid,
            OrderTimelineEntryType.PaymentReceived,
            updatedAtUtc,
            updatedByUserId);
    }

    public void Confirm(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureOrderStatus(
            OrderStatus.Paid);

        SetOrderStatus(
            OrderStatus.Confirmed,
            OrderTimelineEntryType.Confirmed,
            updatedAtUtc,
            updatedByUserId);
    }

    public void StartProcessing(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureOrderStatus(
            OrderStatus.Confirmed);

        Status =
            OrderStatus.Processing;

        FulfillmentStatus =
            OrderFulfillmentStatus.Processing;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);

        RecordTimeline(
            OrderTimelineEntryType.ProcessingStarted,
            updatedAtUtc,
            updatedByUserId);
    }

    public void MarkReadyToShip(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureOrderStatus(
            OrderStatus.Processing);

        EnsureFulfillmentStatus(
            OrderFulfillmentStatus.Processing);

        FulfillmentStatus =
            OrderFulfillmentStatus.ReadyToShip;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);

        RecordTimeline(
            OrderTimelineEntryType.ReadyToShip,
            updatedAtUtc,
            updatedByUserId);
    }

    public void MarkShipped(
        string shippingCarrier,
        string trackingNumber,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureOrderStatus(
            OrderStatus.Processing);

        EnsureFulfillmentStatus(
            OrderFulfillmentStatus.ReadyToShip);

        ShippingCarrier =
            NormalizeRequired(
                shippingCarrier,
                120,
                nameof(shippingCarrier));

        TrackingNumber =
            NormalizeRequired(
                trackingNumber,
                200,
                nameof(trackingNumber));

        FulfillmentStatus =
            OrderFulfillmentStatus.Shipped;

        ShippedAtUtc =
            updatedAtUtc;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);

        RecordTimeline(
            OrderTimelineEntryType.Shipped,
            updatedAtUtc,
            updatedByUserId,
            $"{ShippingCarrier} / {TrackingNumber}");
    }

    public void MarkInTransit(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureOrderStatus(
            OrderStatus.Processing);

        EnsureFulfillmentStatus(
            OrderFulfillmentStatus.Shipped);

        FulfillmentStatus =
            OrderFulfillmentStatus.InTransit;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);

        RecordTimeline(
            OrderTimelineEntryType.InTransit,
            updatedAtUtc,
            updatedByUserId);
    }

    public void MarkDelivered(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureOrderStatus(
            OrderStatus.Processing);

        if (FulfillmentStatus is not
            OrderFulfillmentStatus.Shipped and not
            OrderFulfillmentStatus.InTransit)
        {
            throw new InvalidOperationException(
                $"Order cannot be delivered while fulfillment status is {FulfillmentStatus}.");
        }

        Status =
            OrderStatus.Fulfilled;

        FulfillmentStatus =
            OrderFulfillmentStatus.Delivered;

        DeliveredAtUtc =
            updatedAtUtc;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);

        RecordTimeline(
            OrderTimelineEntryType.Delivered,
            updatedAtUtc,
            updatedByUserId);
    }

    public void MarkFulfilled(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status is not
            OrderStatus.Confirmed and not
            OrderStatus.Processing)
        {
            throw new InvalidOperationException(
                $"Order cannot be fulfilled while status is {Status}.");
        }

        Status =
            OrderStatus.Fulfilled;

        FulfillmentStatus =
            OrderFulfillmentStatus.Fulfilled;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);

        RecordTimeline(
            OrderTimelineEntryType.Fulfilled,
            updatedAtUtc,
            updatedByUserId);
    }

    public void Cancel(
        string? reason,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status ==
            OrderStatus.Cancelled)
        {
            return;
        }

        if (Status ==
                OrderStatus.Fulfilled ||
            FulfillmentStatus is
                OrderFulfillmentStatus.Shipped or
                OrderFulfillmentStatus.InTransit or
                OrderFulfillmentStatus.Delivered or
                OrderFulfillmentStatus.Fulfilled)
        {
            throw new InvalidOperationException(
                "A shipped or fulfilled order cannot be cancelled.");
        }

        CancellationReason =
            NormalizeOptional(
                reason,
                500,
                nameof(reason));

        Status =
            OrderStatus.Cancelled;

        FulfillmentStatus =
            OrderFulfillmentStatus.Cancelled;

        CancelledAtUtc =
            updatedAtUtc;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);

        RecordTimeline(
            OrderTimelineEntryType.Cancelled,
            updatedAtUtc,
            updatedByUserId,
            CancellationReason);
    }

    private void SetOrderStatus(
        OrderStatus status,
        OrderTimelineEntryType timelineType,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        Status =
            status;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);

        RecordTimeline(
            timelineType,
            updatedAtUtc,
            updatedByUserId);
    }

    private void EnsureOrderStatus(
        OrderStatus expected)
    {
        if (Status !=
            expected)
        {
            throw new InvalidOperationException(
                $"Order status must be {expected}, but current status is {Status}.");
        }
    }

    private void EnsureFulfillmentStatus(
        OrderFulfillmentStatus expected)
    {
        if (FulfillmentStatus !=
            expected)
        {
            throw new InvalidOperationException(
                $"Order fulfillment status must be {expected}, but current status is {FulfillmentStatus}.");
        }
    }

    private void MarkUpdated(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        UpdatedAtUtc =
            updatedAtUtc;

        UpdatedByUserId =
            updatedByUserId;
    }

    private void RecordTimeline(
        OrderTimelineEntryType type,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId,
        string? note = null)
    {
        _timeline.Add(
            OrderTimelineEntry.Create(
                TenantId,
                Id,
                type,
                Status,
                FulfillmentStatus,
                createdAtUtc,
                createdByUserId,
                note));
    }

    private static string NormalizeRequired(
        string value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            throw new ArgumentException(
                "Value is required.",
                parameterName);
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