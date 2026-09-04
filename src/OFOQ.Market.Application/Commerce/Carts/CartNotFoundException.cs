namespace OFOQ.Market.Application.Commerce.Carts;

public sealed class CartNotFoundException :
    Exception
{
    public CartNotFoundException()
        : base(
            "The active cart was not found.")
    {
    }
}