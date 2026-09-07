namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentWebhookTargetNotFoundException : Exception
{
    public PaymentWebhookTargetNotFoundException()
        : base("The payment intent targeted by the webhook was not found.")
    {
    }
}
