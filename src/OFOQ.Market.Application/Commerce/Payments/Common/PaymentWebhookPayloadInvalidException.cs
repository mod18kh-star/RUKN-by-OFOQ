namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentWebhookPayloadInvalidException : Exception
{
    public PaymentWebhookPayloadInvalidException(string message)
        : base(message)
    {
    }
}
