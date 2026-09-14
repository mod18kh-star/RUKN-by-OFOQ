using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed record StartMerchantVerificationReviewCommand(
    MerchantVerificationProfileId ProfileId,
    UserId ReviewerUserId);

public sealed record StartMerchantVerificationReviewResult(
    MerchantVerificationProfileId ProfileId,
    TenantId TenantId,
    MerchantVerificationStatus Status,
    DateTimeOffset ReviewStartedAtUtc,
    UserId ReviewerUserId);
