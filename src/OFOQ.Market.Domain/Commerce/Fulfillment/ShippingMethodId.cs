namespace OFOQ.Market.Domain.Commerce.Fulfillment;

public readonly record struct ShippingMethodId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;

    public static ShippingMethodId New() => new(Guid.NewGuid());

    public static ShippingMethodId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Shipping method ID cannot be empty.", nameof(value));

        return new ShippingMethodId(value);
    }

    public override string ToString() => Value.ToString();
}
