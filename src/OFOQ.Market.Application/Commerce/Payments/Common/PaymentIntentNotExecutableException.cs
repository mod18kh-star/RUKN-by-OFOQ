namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentIntentNotExecutableException : Exception
{
    public PaymentIntentNotExecutableException()
        : base("The payment intent is not executable in its current state.")
    {
    }
}
