using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed record MerchantVerificationReviewSummaryResult(
    MerchantVerificationProfileId ProfileId,
    TenantId TenantId,
    UserId PrincipalUserId,
    MerchantVerificationSubjectType SubjectType,
    string CountryCode,
    string LegalName,
    MerchantVerificationStatus Status,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ReviewStartedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    Guid? ReviewedByUserId,
    DateTimeOffset? VerifiedAtUtc,
    DateTimeOffset? ExpiredAtUtc,
    string? ReviewNote,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record MerchantVerificationReviewDocumentFileResult(
    MerchantVerificationDocumentFileId FileId,
    MerchantVerificationDocumentSide Side,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset CreatedAtUtc);

public sealed record MerchantVerificationReviewDocumentResult(
    MerchantVerificationDocumentId DocumentId,
    MerchantVerificationDocumentType DocumentType,
    string IssuingCountryCode,
    string HolderName,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    MerchantVerificationDocumentReviewStatus ReviewStatus,
    string? ReviewNote,
    DateTimeOffset? ReviewedAtUtc,
    Guid? ReviewedByUserId,
    IReadOnlyList<MerchantVerificationReviewDocumentFileResult> Files);

public sealed record MerchantVerificationReviewDetailResult(
    MerchantVerificationReviewSummaryResult Profile,
    IReadOnlyList<MerchantVerificationReviewDocumentResult> Documents);