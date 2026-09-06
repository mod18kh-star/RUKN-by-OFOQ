namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed class CheckoutProductNotAvailableException :
    InvalidOperationException
{
    public CheckoutProductNotAvailableException()
        : base(
            "A product in the cart is no longer available for checkout.")
    {
    }
}
