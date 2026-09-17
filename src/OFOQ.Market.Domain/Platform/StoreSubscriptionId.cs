namespace OFOQ.Market.Domain.Platform;

public readonly record struct StoreSubscriptionId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;

    public static StoreSubscriptionId New() =>
        new(Guid.NewGuid());

    public static StoreSubscriptionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Store subscription ID cannot be empty.",
                nameof(value));
        }

        return new StoreSubscriptionId(value);
    }
}
