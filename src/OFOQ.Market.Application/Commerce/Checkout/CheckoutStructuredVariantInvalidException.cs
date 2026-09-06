namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed class CheckoutStructuredVariantInvalidException :
    InvalidOperationException
{
    public CheckoutStructuredVariantInvalidException()
        : base(
            "A structured product variant in the cart no longer has a valid option selection.")
    {
    }
}
