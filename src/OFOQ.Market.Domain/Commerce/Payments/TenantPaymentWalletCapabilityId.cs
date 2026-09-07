namespace OFOQ.Market.Domain.Commerce.Payments;

public readonly record struct TenantPaymentWalletCapabilityId(
    Guid Value)
{
    public bool IsEmpty =>
        Value == Guid.Empty;

    public static TenantPaymentWalletCapabilityId New()
    {
        return new TenantPaymentWalletCapabilityId(
            Guid.NewGuid());
    }

    public static TenantPaymentWalletCapabilityId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment wallet capability ID cannot be empty.",
                nameof(value));
        }

        return new TenantPaymentWalletCapabilityId(
            value);
    }
}