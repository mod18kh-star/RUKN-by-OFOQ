using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Commerce.Orders;

public sealed class OrderLifecycleTests
{
    [Fact]
    public void PhysicalOrder_CanProgressThroughShippingLifecycle()
    {
        var now =
            DateTimeOffset.UtcNow;

        var actor =
            Guid.NewGuid();

        var order =
            CreateOrder(
                now);

        order.MarkPaid(
            now.AddMinutes(1),
            actor);

        order.Confirm(
            now.AddMinutes(2),
            actor);

        order.StartProcessing(
            now.AddMinutes(3),
            actor);

        order.MarkReadyToShip(
            now.AddMinutes(4),
            actor);

        order.MarkShipped(
            "DHL",
            "TRACK-123",
            now.AddMinutes(5),
            actor);

        order.MarkInTransit(
            now.AddMinutes(6),
            actor);

        order.MarkDelivered(
            now.AddMinutes(7),
            actor);

        Assert.Equal(
            OrderStatus.Fulfilled,
            order.Status);

        Assert.Equal(
            OrderFulfillmentStatus.Delivered,
            order.FulfillmentStatus);

        Assert.Equal(
            "DHL",
            order.ShippingCarrier);

        Assert.Equal(
            "TRACK-123",
            order.TrackingNumber);

        Assert.NotNull(
            order.ShippedAtUtc);

        Assert.NotNull(
            order.DeliveredAtUtc);

        Assert.Equal(
            8,
            order.Timeline.Count);

        Assert.Equal(
            OrderTimelineEntryType.Delivered,
            order.Timeline.Last().Type);
    }

    [Fact]
    public void ProcessingBeforePayment_IsRejected()
    {
        var order =
            CreateOrder(
                DateTimeOffset.UtcNow);

        Assert.Throws<
            InvalidOperationException>(
                () =>
                    order.StartProcessing(
                        DateTimeOffset.UtcNow));

        Assert.Equal(
            OrderStatus.Pending,
            order.Status);

        Assert.Equal(
            OrderFulfillmentStatus.Unfulfilled,
            order.FulfillmentStatus);
    }

    [Fact]
    public void ShippingDetails_AreNormalized()
    {
        var now =
            DateTimeOffset.UtcNow;

        var order =
            CreateOrder(
                now);

        order.MarkPaid(
            now.AddMinutes(1));

        order.Confirm(
            now.AddMinutes(2));

        order.StartProcessing(
            now.AddMinutes(3));

        order.MarkReadyToShip(
            now.AddMinutes(4));

        order.MarkShipped(
            "  Aramex  ",
            "  123-ABC  ",
            now.AddMinutes(5));

        Assert.Equal(
            "Aramex",
            order.ShippingCarrier);

        Assert.Equal(
            "123-ABC",
            order.TrackingNumber);
    }

    [Fact]
    public void UnshippedOrder_CanBeCancelled()
    {
        var now =
            DateTimeOffset.UtcNow;

        var order =
            CreateOrder(
                now);

        order.Cancel(
            "  Customer requested cancellation  ",
            now.AddMinutes(1));

        Assert.Equal(
            OrderStatus.Cancelled,
            order.Status);

        Assert.Equal(
            OrderFulfillmentStatus.Cancelled,
            order.FulfillmentStatus);

        Assert.Equal(
            "Customer requested cancellation",
            order.CancellationReason);

        Assert.NotNull(
            order.CancelledAtUtc);

        Assert.Equal(
            OrderTimelineEntryType.Cancelled,
            order.Timeline.Last().Type);
    }

    [Fact]
    public void ShippedOrder_CannotBeCancelled()
    {
        var now =
            DateTimeOffset.UtcNow;

        var order =
            CreateOrder(
                now);

        order.MarkPaid(
            now.AddMinutes(1));

        order.Confirm(
            now.AddMinutes(2));

        order.StartProcessing(
            now.AddMinutes(3));

        order.MarkReadyToShip(
            now.AddMinutes(4));

        order.MarkShipped(
            "DHL",
            "TRACK-1",
            now.AddMinutes(5));

        Assert.Throws<
            InvalidOperationException>(
                () =>
                    order.Cancel(
                        "Too late",
                        now.AddMinutes(6)));

        Assert.Equal(
            OrderFulfillmentStatus.Shipped,
            order.FulfillmentStatus);
    }

    [Fact]
    public void DuplicateMarkPaid_IsIdempotent()
    {
        var now =
            DateTimeOffset.UtcNow;

        var order =
            CreateOrder(
                now);

        order.MarkPaid(
            now.AddMinutes(1));

        var count =
            order.Timeline.Count;

        order.MarkPaid(
            now.AddMinutes(2));

        Assert.Equal(
            count,
            order.Timeline.Count);

        Assert.Equal(
            OrderStatus.Paid,
            order.Status);
    }

    private static Order CreateOrder(
        DateTimeOffset createdAtUtc)
    {
        return Order.Create(
            TenantId.New(),
            UserId.New(),
            CartId.New(),
            CurrencyCode.Create(
                "SAR"),
            new[]
            {
                new OrderItemSnapshot(
                    ProductId.New(),
                    ProductVariantId.New(),
                    "Test Product",
                    "Default",
                    "SKU-001",
                    Money.Create(
                        100m,
                        "SAR"),
                    1)
            },
            createdAtUtc);
    }
}