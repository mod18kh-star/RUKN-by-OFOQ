namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentAlreadySucceededException :
    InvalidOperationException
{
    public PaymentAlreadySucceededException()
        : base("This order already has a successful payment.")
    {
    }
}
