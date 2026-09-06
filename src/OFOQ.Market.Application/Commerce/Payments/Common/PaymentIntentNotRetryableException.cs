namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentIntentNotRetryableException :
    InvalidOperationException
{
    public PaymentIntentNotRetryableException()
        : base("Only failed, cancelled, or expired payment intents can be retried.")
    {
    }
}
