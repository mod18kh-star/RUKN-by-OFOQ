namespace OFOQ.Market.Contracts.Commerce.Verification;

public sealed record MerchantVerificationProfileRequest(
    string SubjectType,
    string CountryCode,
    string LegalName);

public sealed record MerchantVerificationDocumentRequest(
    string DocumentType,
    string IssuingCountryCode,
    string HolderName,
    string DocumentNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate);

public sealed record MerchantVerificationFileResponse(
    Guid FileId,
    string Side,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset CreatedAtUtc);

public sealed record MerchantVerificationDocumentResponse(
    Guid DocumentId,
    string DocumentType,
    string IssuingCountryCode,
    string HolderName,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string ReviewStatus,
    string? ReviewNote,
    DateTimeOffset? ReviewedAtUtc,
    IReadOnlyList<MerchantVerificationFileResponse> Files);

public sealed record MerchantVerificationProfileResponse(
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
    DateTimeOffset? VerifiedAtUtc,
    DateTimeOffset? ExpiredAtUtc,
    string? ReviewNote);

public sealed record MerchantVerificationSelfServiceResponse(
    MerchantVerificationProfileResponse? Profile,
    IReadOnlyList<MerchantVerificationDocumentResponse> Documents);