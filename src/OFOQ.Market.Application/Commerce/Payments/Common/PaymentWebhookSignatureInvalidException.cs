namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentWebhookSignatureInvalidException : Exception
{
    public PaymentWebhookSignatureInvalidException()
        : base("The payment webhook signature is invalid.")
    {
    }
}
