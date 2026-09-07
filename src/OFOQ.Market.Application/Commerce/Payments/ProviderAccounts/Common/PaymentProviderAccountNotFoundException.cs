namespace OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Common;

public sealed class PaymentProviderAccountNotFoundException :
    Exception
{
    public PaymentProviderAccountNotFoundException()
        : base(
            "The payment provider account was not found.")
    {
    }
}