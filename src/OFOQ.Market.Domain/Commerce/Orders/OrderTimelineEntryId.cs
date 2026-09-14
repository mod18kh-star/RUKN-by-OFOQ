namespace OFOQ.Market.Domain.Commerce.Orders;

public readonly record struct OrderTimelineEntryId(
    Guid Value)
{
    public static OrderTimelineEntryId New() =>
        new(
            Guid.NewGuid());

    public static OrderTimelineEntryId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Order timeline entry ID cannot be empty.",
                nameof(value));
        }

        return new OrderTimelineEntryId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString() =>
        Value.ToString();
}