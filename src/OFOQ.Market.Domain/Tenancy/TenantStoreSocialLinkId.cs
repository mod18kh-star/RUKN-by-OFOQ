namespace OFOQ.Market.Domain.Tenancy;

public readonly record struct TenantStoreSocialLinkId(
    Guid Value)
{
    public bool IsEmpty =>
        Value == Guid.Empty;

    public static TenantStoreSocialLinkId New()
    {
        return new TenantStoreSocialLinkId(
            Guid.NewGuid());
    }

    public static TenantStoreSocialLinkId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant store social-link ID cannot be empty.",
                nameof(value));
        }

        return new TenantStoreSocialLinkId(
            value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
