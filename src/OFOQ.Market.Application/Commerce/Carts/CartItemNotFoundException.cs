namespace OFOQ.Market.Application.Commerce.Carts;

public sealed class CartItemNotFoundException :
    Exception
{
    public CartItemNotFoundException()
        : base(
            "The requested cart item was not found.")
    {
    }
}