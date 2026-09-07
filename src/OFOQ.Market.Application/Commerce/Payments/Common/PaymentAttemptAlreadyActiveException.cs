namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentAttemptAlreadyActiveException : Exception
{
    public PaymentAttemptAlreadyActiveException()
        : base("Another payment attempt is already active for this order.")
    {
    }
}
