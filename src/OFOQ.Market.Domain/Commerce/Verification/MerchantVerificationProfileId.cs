namespace OFOQ.Market.Domain.Commerce.Verification;

public readonly record struct MerchantVerificationProfileId(
    Guid Value)
{
    public bool IsEmpty =>
        Value == Guid.Empty;

    public static MerchantVerificationProfileId New()
    {
        return new MerchantVerificationProfileId(
            Guid.NewGuid());
    }

    public static MerchantVerificationProfileId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Merchant verification profile ID cannot be empty.",
                nameof(value));
        }

        return new MerchantVerificationProfileId(
            value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}