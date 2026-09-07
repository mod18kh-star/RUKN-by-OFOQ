namespace OFOQ.Market.Domain.Commerce.Orders;

public enum OrderStatus
{
    Pending = 0,

    Confirmed = 1,

    Processing = 2,

    Fulfilled = 3,

    Cancelled = 4,

    Paid = 5
}