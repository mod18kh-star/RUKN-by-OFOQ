namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentMethodNotAvailableException :
    InvalidOperationException
{
    public PaymentMethodNotAvailableException()
        : base("The selected payment method is not available for this order.")
    {
    }
}
