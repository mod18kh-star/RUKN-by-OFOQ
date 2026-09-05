namespace OFOQ.Market.Domain.Commerce.Orders;

public readonly record struct OrderItemId(
    Guid Value)
{
    public static OrderItemId New()
    {
        return new OrderItemId(
            Guid.NewGuid());
    }

    public static OrderItemId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Order item ID cannot be empty.",
                nameof(value));
        }

        return new OrderItemId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}