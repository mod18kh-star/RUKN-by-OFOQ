namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed class CheckoutCartNotFoundException :
    InvalidOperationException
{
    public CheckoutCartNotFoundException()
        : base(
            "No active cart is available for checkout.")
    {
    }
}
