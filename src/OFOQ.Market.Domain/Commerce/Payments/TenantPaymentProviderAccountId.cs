namespace OFOQ.Market.Domain.Commerce.Payments;

public readonly record struct TenantPaymentProviderAccountId(
    Guid Value)
{
    public bool IsEmpty =>
        Value == Guid.Empty;

    public static TenantPaymentProviderAccountId New()
    {
        return new TenantPaymentProviderAccountId(
            Guid.NewGuid());
    }

    public static TenantPaymentProviderAccountId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment provider account ID cannot be empty.",
                nameof(value));
        }

        return new TenantPaymentProviderAccountId(
            value);
    }
}