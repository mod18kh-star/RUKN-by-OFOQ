using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed record RequestMoreInformationMerchantVerificationReviewCommand(
    MerchantVerificationProfileId ProfileId,
    UserId ReviewerUserId,
    string ReviewNote);

public sealed record RequestMoreInformationMerchantVerificationReviewResult(
    MerchantVerificationProfileId ProfileId,
    TenantId TenantId,
    MerchantVerificationStatus Status,
    DateTimeOffset ReviewedAtUtc,
    UserId ReviewerUserId,
    string ReviewNote);