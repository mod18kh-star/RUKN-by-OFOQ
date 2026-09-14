namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed class MerchantVerificationReviewConcurrencyException :
    Exception
{
    private const string DefaultMessage =
        "The merchant verification profile was changed by another operation. Reload it and retry.";

    public MerchantVerificationReviewConcurrencyException()
        : base(
            DefaultMessage)
    {
    }

    public MerchantVerificationReviewConcurrencyException(
        Exception innerException)
        : base(
            DefaultMessage,
            innerException)
    {
        ArgumentNullException.ThrowIfNull(
            innerException);
    }
}