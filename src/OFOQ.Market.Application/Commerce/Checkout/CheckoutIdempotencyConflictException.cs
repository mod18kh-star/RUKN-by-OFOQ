namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed class CheckoutIdempotencyConflictException :
    InvalidOperationException
{
    public CheckoutIdempotencyConflictException()
        : base(
            "The idempotency key has already been used for a different checkout request.")
    {
    }
}
