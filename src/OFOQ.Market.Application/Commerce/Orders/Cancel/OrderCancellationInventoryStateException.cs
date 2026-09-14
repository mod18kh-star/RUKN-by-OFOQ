namespace OFOQ.Market.Application.Commerce.Orders.Cancel;

public sealed class OrderCancellationInventoryStateException :
    InvalidOperationException
{
    public OrderCancellationInventoryStateException()
        : base(
            "The order inventory state could not be restored safely.")
    {
    }
}