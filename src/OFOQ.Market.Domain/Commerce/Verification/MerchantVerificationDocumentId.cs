namespace OFOQ.Market.Domain.Commerce.Verification;

public readonly record struct MerchantVerificationDocumentId(
    Guid Value)
{
    public bool IsEmpty =>
        Value == Guid.Empty;

    public static MerchantVerificationDocumentId New()
    {
        return new MerchantVerificationDocumentId(
            Guid.NewGuid());
    }

    public static MerchantVerificationDocumentId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Merchant verification document ID cannot be empty.",
                nameof(value));
        }

        return new MerchantVerificationDocumentId(
            value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}