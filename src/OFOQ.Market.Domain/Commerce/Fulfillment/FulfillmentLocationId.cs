namespace OFOQ.Market.Domain.Commerce.Fulfillment;

public readonly record struct FulfillmentLocationId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;

    public static FulfillmentLocationId New() => new(Guid.NewGuid());

    public static FulfillmentLocationId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Fulfillment location ID cannot be empty.", nameof(value));

        return new FulfillmentLocationId(value);
    }

    public override string ToString() => Value.ToString();
}
