namespace OFOQ.Market.Domain.Commerce.Orders;

public enum OrderFulfillmentStatus
{
    Unfulfilled = 0,

    Processing = 1,

    ReadyToShip = 2,

    Shipped = 3,

    InTransit = 4,

    Delivered = 5,

    Fulfilled = 6,

    Cancelled = 7
}