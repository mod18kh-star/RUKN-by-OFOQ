namespace OFOQ.Market.Domain.Commerce.Payments;

public enum PaymentIntentStatus
{
    Pending = 0,
    RequiresAction = 1,
    Processing = 2,
    Succeeded = 3,
    Failed = 4,
    Cancelled = 5,
    Expired = 6
}
