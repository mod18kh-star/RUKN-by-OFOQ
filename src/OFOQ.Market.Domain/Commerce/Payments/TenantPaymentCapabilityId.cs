namespace OFOQ.Market.Domain.Commerce.Payments;

public readonly record struct TenantPaymentCapabilityId(Guid Value)
{
    public static TenantPaymentCapabilityId New() => new(Guid.NewGuid());

    public static TenantPaymentCapabilityId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant payment capability ID cannot be empty.",
                nameof(value));
        }

        return new TenantPaymentCapabilityId(value);
    }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
