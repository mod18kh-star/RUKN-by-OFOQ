namespace OFOQ.Market.Application.Commerce.Carts;

public sealed class CartVariantNotAvailableException :
    Exception
{
    public CartVariantNotAvailableException()
        : base(
            "The requested product variant is not available for purchase.")
    {
    }
}