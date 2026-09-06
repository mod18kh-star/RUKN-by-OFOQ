namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed class CheckoutCartEmptyException :
    InvalidOperationException
{
    public CheckoutCartEmptyException()
        : base(
            "The active cart is empty and cannot be checked out.")
    {
    }
}
