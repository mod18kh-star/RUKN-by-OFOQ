namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentProviderResultInvalidException : Exception
{
    public PaymentProviderResultInvalidException(string message)
        : base(message)
    {
    }
}
