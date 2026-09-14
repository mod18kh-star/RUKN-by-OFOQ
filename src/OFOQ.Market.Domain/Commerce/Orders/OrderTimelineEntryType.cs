namespace OFOQ.Market.Domain.Commerce.Orders;

public enum OrderTimelineEntryType
{
    Created = 0,

    PaymentReceived = 1,

    Confirmed = 2,

    ProcessingStarted = 3,

    ReadyToShip = 4,

    Shipped = 5,

    InTransit = 6,

    Delivered = 7,

    Fulfilled = 8,

    Cancelled = 9
}