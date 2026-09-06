namespace OFOQ.Market.Domain.Commerce.Payments;

public readonly record struct TenantPaymentMethodId(Guid Value)
{
    public static TenantPaymentMethodId New() => new(Guid.NewGuid());

    public static TenantPaymentMethodId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant payment method ID cannot be empty.",
                nameof(value));
        }

        return new TenantPaymentMethodId(value);
    }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
