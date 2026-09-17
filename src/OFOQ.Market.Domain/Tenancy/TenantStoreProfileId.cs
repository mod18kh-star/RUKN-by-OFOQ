namespace OFOQ.Market.Domain.Tenancy;

public readonly record struct TenantStoreProfileId(
    Guid Value)
{
    public bool IsEmpty =>
        Value == Guid.Empty;

    public static TenantStoreProfileId New()
    {
        return new TenantStoreProfileId(
            Guid.NewGuid());
    }

    public static TenantStoreProfileId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant store profile ID cannot be empty.",
                nameof(value));
        }

        return new TenantStoreProfileId(
            value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
