namespace OFOQ.Market.Domain.Commerce.Configuration;

public readonly record struct TenantCommerceCapabilityOverrideId(
    Guid Value)
{
    public bool IsEmpty =>
        Value == Guid.Empty;

    public static TenantCommerceCapabilityOverrideId New()
    {
        return new TenantCommerceCapabilityOverrideId(
            Guid.NewGuid());
    }

    public static TenantCommerceCapabilityOverrideId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant commerce capability override ID cannot be empty.",
                nameof(value));
        }

        return new TenantCommerceCapabilityOverrideId(
            value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}