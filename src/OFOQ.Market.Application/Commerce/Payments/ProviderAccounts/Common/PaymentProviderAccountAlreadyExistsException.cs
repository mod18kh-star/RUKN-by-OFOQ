namespace OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Common;

public sealed class PaymentProviderAccountAlreadyExistsException :
    Exception
{
    public PaymentProviderAccountAlreadyExistsException()
        : base(
            "A payment provider account with the same provider and environment already exists.")
    {
    }

    public PaymentProviderAccountAlreadyExistsException(
        string message)
        : base(message)
    {
    }
}