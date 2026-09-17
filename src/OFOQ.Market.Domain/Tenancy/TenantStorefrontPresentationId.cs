namespace OFOQ.Market.Domain.Tenancy;

public readonly record struct TenantStorefrontPresentationId(
    Guid Value)
{
    public bool IsEmpty =>
        Value == Guid.Empty;

    public static TenantStorefrontPresentationId New()
    {
        return new TenantStorefrontPresentationId(
            Guid.NewGuid());
    }

    public static TenantStorefrontPresentationId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant storefront presentation ID cannot be empty.",
                nameof(value));
        }

        return new TenantStorefrontPresentationId(
            value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
