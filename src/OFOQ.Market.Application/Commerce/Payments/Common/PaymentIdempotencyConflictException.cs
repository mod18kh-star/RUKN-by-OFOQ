namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed class PaymentIdempotencyConflictException :
    InvalidOperationException
{
    public PaymentIdempotencyConflictException()
        : base("The Idempotency-Key is already associated with a different payment request.")
    {
    }
}
