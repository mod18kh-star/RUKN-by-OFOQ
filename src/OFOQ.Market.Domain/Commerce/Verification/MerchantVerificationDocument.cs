using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Verification;

public sealed class MerchantVerificationDocument :
    AggregateRoot<MerchantVerificationDocumentId>,
    ITenantDataScoped,
    IAuditable
{
    public const int MaxHolderNameLength =
        200;

    public const int MaxProtectedDocumentNumberLength =
        4096;

    public const int DocumentNumberFingerprintLength =
        64;

    public const int MaxReviewNoteLength =
        2000;

    private MerchantVerificationDocument()
    {
    }

    private MerchantVerificationDocument(
        MerchantVerificationDocumentId id,
        TenantId tenantId,
        MerchantVerificationProfileId profileId,
        MerchantVerificationDocumentType documentType,
        string issuingCountryCode,
        string holderName,
        string protectedDocumentNumber,
        string documentNumberFingerprint,
        DateOnly? issueDate,
        DateOnly? expiryDate,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        ValidateTenantId(
            tenantId);

        ValidateProfileId(
            profileId);

        ValidateDocumentType(
            documentType);

        TenantId =
            tenantId;

        ProfileId =
            profileId;

        DocumentType =
            documentType;

        IssuingCountryCode =
            NormalizeCountryCode(
                issuingCountryCode);

        HolderName =
            NormalizeHolderName(
                holderName);

        ProtectedDocumentNumber =
            NormalizeProtectedDocumentNumber(
                protectedDocumentNumber);

        DocumentNumberFingerprint =
            NormalizeFingerprint(
                documentNumberFingerprint);

        ValidateDates(
            issueDate,
            expiryDate);

        IssueDate =
            issueDate;

        ExpiryDate =
            expiryDate;

        ReviewStatus =
            MerchantVerificationDocumentReviewStatus.PendingReview;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public MerchantVerificationProfileId ProfileId { get; private set; }

    public MerchantVerificationDocumentType DocumentType { get; private set; }

    public string IssuingCountryCode { get; private set; } =
        string.Empty;

    public string HolderName { get; private set; } =
        string.Empty;

    public string ProtectedDocumentNumber { get; private set; } =
        string.Empty;

    public string DocumentNumberFingerprint { get; private set; } =
        string.Empty;

    public DateOnly? IssueDate { get; private set; }

    public DateOnly? ExpiryDate { get; private set; }

    public MerchantVerificationDocumentReviewStatus ReviewStatus
    {
        get;
        private set;
    }

    public string? ReviewNote { get; private set; }

    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static MerchantVerificationDocument Create(
        TenantId tenantId,
        MerchantVerificationProfileId profileId,
        MerchantVerificationDocumentType documentType,
        string issuingCountryCode,
        string holderName,
        string protectedDocumentNumber,
        string documentNumberFingerprint,
        DateOnly? issueDate,
        DateOnly? expiryDate,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new MerchantVerificationDocument(
            MerchantVerificationDocumentId.New(),
            tenantId,
            profileId,
            documentType,
            issuingCountryCode,
            holderName,
            protectedDocumentNumber,
            documentNumberFingerprint,
            issueDate,
            expiryDate,
            createdAtUtc,
            createdByUserId);
    }

    public void UpdateDetails(
        MerchantVerificationDocumentType documentType,
        string issuingCountryCode,
        string holderName,
        string protectedDocumentNumber,
        string documentNumberFingerprint,
        DateOnly? issueDate,
        DateOnly? expiryDate,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (ReviewStatus ==
                MerchantVerificationDocumentReviewStatus.Accepted)
        {
            throw new InvalidOperationException(
                "An accepted merchant verification document cannot be edited.");
        }

        ValidateDocumentType(
            documentType);

        DocumentType =
            documentType;

        IssuingCountryCode =
            NormalizeCountryCode(
                issuingCountryCode);

        HolderName =
            NormalizeHolderName(
                holderName);

        ProtectedDocumentNumber =
            NormalizeProtectedDocumentNumber(
                protectedDocumentNumber);

        DocumentNumberFingerprint =
            NormalizeFingerprint(
                documentNumberFingerprint);

        ValidateDates(
            issueDate,
            expiryDate);

        IssueDate =
            issueDate;

        ExpiryDate =
            expiryDate;

        ReviewStatus =
            MerchantVerificationDocumentReviewStatus.PendingReview;

        ReviewNote =
            null;

        ReviewedAtUtc =
            null;

        ReviewedByUserId =
            null;

        Touch(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Accept(
        DateTimeOffset reviewedAtUtc,
        Guid reviewedByUserId,
        string? reviewNote = null)
    {
        ValidateReviewer(
            reviewedByUserId);

        ReviewStatus =
            MerchantVerificationDocumentReviewStatus.Accepted;

        ReviewNote =
            string.IsNullOrWhiteSpace(
                reviewNote)
                ? null
                : NormalizeReviewNote(
                    reviewNote);

        ReviewedAtUtc =
            reviewedAtUtc;

        ReviewedByUserId =
            reviewedByUserId;

        Touch(
            reviewedAtUtc,
            reviewedByUserId);
    }

    public void Reject(
        string reviewNote,
        DateTimeOffset reviewedAtUtc,
        Guid reviewedByUserId)
    {
        ValidateReviewer(
            reviewedByUserId);

        ReviewStatus =
            MerchantVerificationDocumentReviewStatus.Rejected;

        ReviewNote =
            NormalizeReviewNote(
                reviewNote);

        ReviewedAtUtc =
            reviewedAtUtc;

        ReviewedByUserId =
            reviewedByUserId;

        Touch(
            reviewedAtUtc,
            reviewedByUserId);
    }

    public void MarkExpired(
        DateTimeOffset expiredAtUtc,
        Guid? actorUserId = null)
    {
        if (ReviewStatus !=
            MerchantVerificationDocumentReviewStatus.Accepted)
        {
            throw new InvalidOperationException(
                "Only an accepted merchant verification document can be marked expired.");
        }

        ReviewStatus =
            MerchantVerificationDocumentReviewStatus.Expired;

        ReviewNote =
            null;

        ReviewedAtUtc =
            expiredAtUtc;

        ReviewedByUserId =
            actorUserId;

        Touch(
            expiredAtUtc,
            actorUserId);
    }

    public bool IsExpiredOn(
        DateOnly date)
    {
        return ExpiryDate.HasValue &&
               ExpiryDate.Value <
               date;
    }

    private void Touch(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        UpdatedAtUtc =
            updatedAtUtc;

        UpdatedByUserId =
            updatedByUserId;
    }

    private static void ValidateTenantId(
        TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }
    }

    private static void ValidateProfileId(
        MerchantVerificationProfileId profileId)
    {
        if (profileId.IsEmpty)
        {
            throw new ArgumentException(
                "Merchant verification profile ID cannot be empty.",
                nameof(profileId));
        }
    }

    private static void ValidateDocumentType(
        MerchantVerificationDocumentType documentType)
    {
        if (documentType ==
                MerchantVerificationDocumentType.Unknown ||
            !Enum.IsDefined(
                documentType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(documentType),
                "A supported merchant verification document type is required.");
        }
    }

    private static void ValidateReviewer(
        Guid reviewedByUserId)
    {
        if (reviewedByUserId ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Reviewer user ID cannot be empty.",
                nameof(reviewedByUserId));
        }
    }

    private static string NormalizeCountryCode(
        string countryCode)
    {
        if (string.IsNullOrWhiteSpace(
                countryCode))
        {
            throw new ArgumentException(
                "Issuing country code is required.",
                nameof(countryCode));
        }

        var normalized =
            countryCode
                .Trim()
                .ToUpperInvariant();

        if (normalized.Length != 2 ||
            !normalized.All(
                char.IsLetter))
        {
            throw new ArgumentException(
                "Issuing country code must be a two-letter ISO country code.",
                nameof(countryCode));
        }

        return normalized;
    }

    private static string NormalizeHolderName(
        string holderName)
    {
        if (string.IsNullOrWhiteSpace(
                holderName))
        {
            throw new ArgumentException(
                "Document holder name is required.",
                nameof(holderName));
        }

        var normalized =
            holderName.Trim();

        if (normalized.Length >
            MaxHolderNameLength)
        {
            throw new ArgumentException(
                $"Document holder name cannot exceed {MaxHolderNameLength} characters.",
                nameof(holderName));
        }

        return normalized;
    }

    private static string NormalizeProtectedDocumentNumber(
        string protectedDocumentNumber)
    {
        if (string.IsNullOrWhiteSpace(
                protectedDocumentNumber))
        {
            throw new ArgumentException(
                "Protected document number is required.",
                nameof(protectedDocumentNumber));
        }

        var normalized =
            protectedDocumentNumber.Trim();

        if (normalized.Length >
            MaxProtectedDocumentNumberLength)
        {
            throw new ArgumentException(
                $"Protected document number cannot exceed {MaxProtectedDocumentNumberLength} characters.",
                nameof(protectedDocumentNumber));
        }

        return normalized;
    }

    private static string NormalizeFingerprint(
        string documentNumberFingerprint)
    {
        if (string.IsNullOrWhiteSpace(
                documentNumberFingerprint))
        {
            throw new ArgumentException(
                "Document number fingerprint is required.",
                nameof(documentNumberFingerprint));
        }

        var normalized =
            documentNumberFingerprint
                .Trim()
                .ToLowerInvariant();

        if (normalized.Length !=
                DocumentNumberFingerprintLength ||
            !normalized.All(
                IsHexCharacter))
        {
            throw new ArgumentException(
                "Document number fingerprint must be a 64-character hexadecimal value.",
                nameof(documentNumberFingerprint));
        }

        return normalized;
    }

    private static void ValidateDates(
        DateOnly? issueDate,
        DateOnly? expiryDate)
    {
        if (issueDate.HasValue &&
            expiryDate.HasValue &&
            expiryDate.Value <
            issueDate.Value)
        {
            throw new ArgumentException(
                "Document expiry date cannot be earlier than its issue date.");
        }
    }

    private static string NormalizeReviewNote(
        string reviewNote)
    {
        if (string.IsNullOrWhiteSpace(
                reviewNote))
        {
            throw new ArgumentException(
                "A review note is required.",
                nameof(reviewNote));
        }

        var normalized =
            reviewNote.Trim();

        if (normalized.Length >
            MaxReviewNoteLength)
        {
            throw new ArgumentException(
                $"Review note cannot exceed {MaxReviewNoteLength} characters.",
                nameof(reviewNote));
        }

        return normalized;
    }

    private static bool IsHexCharacter(
        char value)
    {
        return value is >= '0' and <= '9'
            or >= 'a' and <= 'f';
    }
}