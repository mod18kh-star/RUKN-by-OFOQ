namespace OFOQ.Market.Domain.Commerce.Verification;

public readonly record struct MerchantVerificationDocumentFileId(
    Guid Value)
{
    public bool IsEmpty =>
        Value == Guid.Empty;

    public static MerchantVerificationDocumentFileId New()
    {
        return new MerchantVerificationDocumentFileId(
            Guid.NewGuid());
    }

    public static MerchantVerificationDocumentFileId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Merchant verification document file ID cannot be empty.",
                nameof(value));
        }

        return new MerchantVerificationDocumentFileId(
            value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}