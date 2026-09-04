namespace OFOQ.Market.Application.Commerce.Carts;

public sealed class CartInsufficientStockException :
    Exception
{
    public CartInsufficientStockException()
        : base(
            "The requested quantity exceeds the available stock.")
    {
    }
}