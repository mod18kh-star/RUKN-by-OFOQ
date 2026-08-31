namespace OFOQ.Market.Domain.Tenancy;

public readonly record struct TenantDomainId(Guid Value)
{
    public static TenantDomainId New()
        => new(Guid.NewGuid());

    public static TenantDomainId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant domain ID cannot be empty.",
                nameof(value));
        }

        return new TenantDomainId(value);
    }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString()
        => Value.ToString();
}