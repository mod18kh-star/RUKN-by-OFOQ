using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed record VerifyMerchantVerificationReviewCommand(
    MerchantVerificationProfileId ProfileId,
    UserId ReviewerUserId,
    string? ReviewNote = null);

public sealed record VerifyMerchantVerificationReviewResult(
    MerchantVerificationProfileId ProfileId,
    TenantId TenantId,
    MerchantVerificationStatus Status,
    DateTimeOffset VerifiedAtUtc,
    UserId ReviewerUserId,
    string? ReviewNote);