namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentIntentNotFoundException :
    KeyNotFoundException
{
    public PaymentIntentNotFoundException()
        : base("The payment intent was not found.")
    {
    }
}
