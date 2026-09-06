namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed class CheckoutInsufficientStockException :
    InvalidOperationException
{
    public CheckoutInsufficientStockException()
        : base(
            "A product variant in the cart does not have enough inventory for checkout.")
    {
    }
}
