namespace OFOQ.Market.Domain.Tenancy;

public readonly record struct TenantId(Guid Value)
{
    public static TenantId New()
        => new(Guid.NewGuid());

    public static TenantId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(value));

        return new TenantId(value);
    }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}