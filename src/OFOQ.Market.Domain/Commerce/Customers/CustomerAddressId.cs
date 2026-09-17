namespace OFOQ.Market.Domain.Commerce.Customers;

public readonly record struct CustomerAddressId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;

    public static CustomerAddressId New() => new(Guid.NewGuid());

    public static CustomerAddressId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Customer address ID cannot be empty.", nameof(value));

        return new CustomerAddressId(value);
    }

    public override string ToString() => Value.ToString();
}
