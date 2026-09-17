namespace OFOQ.Market.Domain.Commerce.Customers;

public readonly record struct CustomerProfileId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;

    public static CustomerProfileId New() => new(Guid.NewGuid());

    public static CustomerProfileId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Customer profile ID cannot be empty.", nameof(value));

        return new CustomerProfileId(value);
    }

    public override string ToString() => Value.ToString();
}
