namespace OFOQ.Market.Domain.Commerce.Verification;

public enum MerchantVerificationStatus
{
    Unknown = 0,

    Draft = 10,

    Submitted = 20,

    UnderReview = 30,

    RequiresMoreInformation = 40,

    Verified = 50,

    Rejected = 60,

    Expired = 70
}