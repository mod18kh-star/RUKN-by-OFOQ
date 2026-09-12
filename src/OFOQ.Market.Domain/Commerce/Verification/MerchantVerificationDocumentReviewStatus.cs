namespace OFOQ.Market.Domain.Commerce.Verification;

public enum MerchantVerificationDocumentReviewStatus
{
    Unknown = 0,

    PendingReview = 10,

    Accepted = 20,

    Rejected = 30,

    Expired = 40
}