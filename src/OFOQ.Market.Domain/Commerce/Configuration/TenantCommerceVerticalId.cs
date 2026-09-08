namespace OFOQ.Market.Domain.Commerce.Configuration;

public readonly record struct TenantCommerceVerticalId(
    Guid Value)
{
    public bool IsEmpty =>
        Value == Guid.Empty;

    public static TenantCommerceVerticalId New()
    {
        return new TenantCommerceVerticalId(
            Guid.NewGuid());
    }

    public static TenantCommerceVerticalId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant commerce vertical ID cannot be empty.",
                nameof(value));
        }

        return new TenantCommerceVerticalId(
            value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}