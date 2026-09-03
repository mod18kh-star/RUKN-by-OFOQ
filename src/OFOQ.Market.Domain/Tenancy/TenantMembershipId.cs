namespace OFOQ.Market.Domain.Tenancy;

public readonly record struct TenantMembershipId(Guid Value)
{
    public static TenantMembershipId New()
        => new(Guid.NewGuid());

    public static TenantMembershipId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(
                "Tenant membership ID cannot be empty.",
                nameof(value));

        return new TenantMembershipId(value);
    }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString()
        => Value.ToString();
}