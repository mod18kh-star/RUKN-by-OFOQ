namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed class MerchantVerificationReviewConflictException :
    Exception
{
    public MerchantVerificationReviewConflictException()
        : base(
            "The reviewer is not permitted to review this merchant verification profile.")
    {
    }
}
