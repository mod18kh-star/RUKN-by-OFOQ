namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentProviderNotConfiguredException : Exception
{
    public PaymentProviderNotConfiguredException(string providerCode)
        : base($"Payment provider '{providerCode}' is not configured.")
    {
    }
}
