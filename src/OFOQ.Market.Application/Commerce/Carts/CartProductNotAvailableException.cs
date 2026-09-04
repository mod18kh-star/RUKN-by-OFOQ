namespace OFOQ.Market.Application.Commerce.Carts;

public sealed class CartProductNotAvailableException :
    Exception
{
    public CartProductNotAvailableException()
        : base(
            "The requested product is not available for purchase.")
    {
    }
}