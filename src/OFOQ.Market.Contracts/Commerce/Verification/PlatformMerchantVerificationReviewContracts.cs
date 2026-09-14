namespace OFOQ.Market.Contracts.Commerce.Verification;

public sealed record PlatformMerchantVerificationReviewSummaryResponse(
    Guid ProfileId,
    Guid TenantId,
    Guid PrincipalUserId,
    string SubjectType,
    string CountryCode,
    string LegalName,
    string Status,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ReviewStartedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    Guid? ReviewedByUserId,
    DateTimeOffset? VerifiedAtUtc,
    DateTimeOffset? ExpiredAtUtc,
    string? ReviewNote,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record PlatformMerchantVerificationDocumentFileResponse(
    Guid FileId,
    string Side,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset CreatedAtUtc);

public sealed record PlatformMerchantVerificationDocumentResponse(
    Guid DocumentId,
    string DocumentType,
    string IssuingCountryCode,
    string HolderName,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string ReviewStatus,
    string? ReviewNote,
    DateTimeOffset? ReviewedAtUtc,
    Guid? ReviewedByUserId,
    IReadOnlyList<PlatformMerchantVerificationDocumentFileResponse> Files);

public sealed record PlatformMerchantVerificationReviewDetailResponse(
    PlatformMerchantVerificationReviewSummaryResponse Profile,
    IReadOnlyList<PlatformMerchantVerificationDocumentResponse> Documents);

public sealed record PlatformMerchantVerificationRequiredNoteRequest(
    string ReviewNote);

public sealed record PlatformMerchantVerificationVerifyRequest(
    string? ReviewNote);

public sealed record PlatformMerchantVerificationReviewActionResponse(
    Guid ProfileId,
    Guid TenantId,
    string Status,
    DateTimeOffset OccurredAtUtc,
    Guid ReviewerUserId,
    string? ReviewNote);