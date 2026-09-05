namespace OFOQ.Market.Domain.Commerce.Orders;

public readonly record struct OrderId(
    Guid Value)
{
    public static OrderId New()
    {
        return new OrderId(
            Guid.NewGuid());
    }

    public static OrderId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Order ID cannot be empty.",
                nameof(value));
        }

        return new OrderId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}