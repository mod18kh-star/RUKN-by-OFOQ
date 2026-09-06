namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed class CheckoutVariantNotAvailableException :
    InvalidOperationException
{
    public CheckoutVariantNotAvailableException()
        : base(
            "A product variant in the cart is no longer available for checkout.")
    {
    }
}
