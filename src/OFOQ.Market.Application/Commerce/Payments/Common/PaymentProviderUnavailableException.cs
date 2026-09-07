namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentProviderUnavailableException : Exception
{
    public PaymentProviderUnavailableException(string providerCode, Exception innerException)
        : base($"Payment provider '{providerCode}' is temporarily unavailable.", innerException)
    {
    }
}
