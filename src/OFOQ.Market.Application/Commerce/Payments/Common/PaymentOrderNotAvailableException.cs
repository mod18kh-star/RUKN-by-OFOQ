namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentOrderNotAvailableException :
    InvalidOperationException
{
    public PaymentOrderNotAvailableException()
        : base("The order is not available for payment.")
    {
    }
}
