namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentWebhookAmountMismatchException : Exception
{
    public PaymentWebhookAmountMismatchException()
        : base("The payment webhook amount or currency does not match the payment intent.")
    {
    }
}
