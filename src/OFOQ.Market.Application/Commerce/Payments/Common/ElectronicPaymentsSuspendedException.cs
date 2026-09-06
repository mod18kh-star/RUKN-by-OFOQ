namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class ElectronicPaymentsSuspendedException :
    InvalidOperationException
{
    public ElectronicPaymentsSuspendedException()
        : base("Electronic payments are currently suspended for this store.")
    {
    }
}
