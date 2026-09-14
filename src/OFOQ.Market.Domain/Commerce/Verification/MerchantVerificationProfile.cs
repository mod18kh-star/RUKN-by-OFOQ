using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Verification;

public sealed class MerchantVerificationProfile :
    AggregateRoot<MerchantVerificationProfileId>,
    ITenantDataScoped,
    IAuditable
{
    public const int MaxLegalNameLength =
        200;

    public const int MaxReviewNoteLength =
        2000;

    private MerchantVerificationProfile()
    {
    }

    private MerchantVerificationProfile(
        MerchantVerificationProfileId id,
        TenantId tenantId,
        UserId principalUserId,
        MerchantVerificationSubjectType subjectType,
        string countryCode,
        string legalName,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        ValidateTenantId(
            tenantId);

        ValidatePrincipalUserId(
            principalUserId);

        ValidateSubjectType(
            subjectType);

        TenantId =
            tenantId;

        PrincipalUserId =
            principalUserId;

        SubjectType =
            subjectType;

        CountryCode =
            NormalizeCountryCode(
                countryCode);

        LegalName =
            NormalizeLegalName(
                legalName);

        Status =
            MerchantVerificationStatus.Draft;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public UserId PrincipalUserId { get; private set; }

    public MerchantVerificationSubjectType SubjectType { get; private set; }

    public string CountryCode { get; private set; } =
        string.Empty;

    public string LegalName { get; private set; } =
        string.Empty;

    public MerchantVerificationStatus Status { get; private set; }

    public DateTimeOffset? SubmittedAtUtc { get; private set; }

    public DateTimeOffset? ReviewStartedAtUtc { get; private set; }

    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public DateTimeOffset? VerifiedAtUtc { get; private set; }

    public DateTimeOffset? ExpiredAtUtc { get; private set; }

    public string? ReviewNote { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static MerchantVerificationProfile Create(
        TenantId tenantId,
        UserId principalUserId,
        MerchantVerificationSubjectType subjectType,
        string countryCode,
        string legalName,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new MerchantVerificationProfile(
            MerchantVerificationProfileId.New(),
            tenantId,
            principalUserId,
            subjectType,
            countryCode,
            legalName,
            createdAtUtc,
            createdByUserId);
    }

    public void UpdateDetails(
        MerchantVerificationSubjectType subjectType,
        string countryCode,
        string legalName,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureEditable();

        ValidateSubjectType(
            subjectType);

        SubjectType =
            subjectType;

        CountryCode =
            NormalizeCountryCode(
                countryCode);

        LegalName =
            NormalizeLegalName(
                legalName);

        Touch(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Submit(
        DateTimeOffset submittedAtUtc,
        Guid? submittedByUserId = null)
    {
        if (Status is not
            (MerchantVerificationStatus.Draft
            or MerchantVerificationStatus.RequiresMoreInformation
            or MerchantVerificationStatus.Rejected
            or MerchantVerificationStatus.Expired))
        {
            throw new InvalidOperationException(
                "The merchant verification profile cannot be submitted from its current state.");
        }

        Status =
            MerchantVerificationStatus.Submitted;

        SubmittedAtUtc =
            submittedAtUtc;

        ReviewStartedAtUtc =
            null;

        ReviewedAtUtc =
            null;

        ReviewedByUserId =
            null;

        VerifiedAtUtc =
            null;

        ExpiredAtUtc =
            null;

        ReviewNote =
            null;

        Touch(
            submittedAtUtc,
            submittedByUserId);
    }

    public void StartReview(
        DateTimeOffset reviewedAtUtc,
        Guid reviewedByUserId)
    {
        if (reviewedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Reviewer user ID cannot be empty.",
                nameof(reviewedByUserId));
        }

        if (Status !=
            MerchantVerificationStatus.Submitted)
        {
            throw new InvalidOperationException(
                "Only a submitted merchant verification profile can enter review.");
        }

        Status =
            MerchantVerificationStatus.UnderReview;

        ReviewStartedAtUtc =
            reviewedAtUtc;

        ReviewedByUserId =
            reviewedByUserId;

        ReviewNote =
            null;

        Touch(
            reviewedAtUtc,
            reviewedByUserId);
    }

    public void RequestMoreInformation(
        string reviewNote,
        DateTimeOffset reviewedAtUtc,
        Guid reviewedByUserId)
    {
        EnsureReviewable(
            reviewedByUserId);

        var normalizedReviewNote =
            NormalizeReviewNote(
                reviewNote);

        Status =
            MerchantVerificationStatus.RequiresMoreInformation;

        ReviewedAtUtc =
            reviewedAtUtc;

        ReviewedByUserId =
            reviewedByUserId;

        ReviewNote =
            normalizedReviewNote;

        Touch(
            reviewedAtUtc,
            reviewedByUserId);
    }

    public void Reject(
        string reviewNote,
        DateTimeOffset reviewedAtUtc,
        Guid reviewedByUserId)
    {
        EnsureReviewable(
            reviewedByUserId);

        var normalizedReviewNote =
            NormalizeReviewNote(
                reviewNote);

        Status =
            MerchantVerificationStatus.Rejected;

        ReviewedAtUtc =
            reviewedAtUtc;

        ReviewedByUserId =
            reviewedByUserId;

        VerifiedAtUtc =
            null;

        ReviewNote =
            normalizedReviewNote;

        Touch(
            reviewedAtUtc,
            reviewedByUserId);
    }

    public void Verify(
        DateTimeOffset verifiedAtUtc,
        Guid reviewedByUserId,
        string? reviewNote = null)
    {
        if (reviewedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Reviewer user ID cannot be empty.",
                nameof(reviewedByUserId));
        }

        if (Status !=
            MerchantVerificationStatus.UnderReview)
        {
            throw new InvalidOperationException(
                "Only a merchant verification profile under review can be verified.");
        }

        var normalizedReviewNote =
            string.IsNullOrWhiteSpace(
                reviewNote)
                ? null
                : NormalizeReviewNote(
                    reviewNote);

        Status =
            MerchantVerificationStatus.Verified;

        ReviewedAtUtc =
            verifiedAtUtc;

        ReviewedByUserId =
            reviewedByUserId;

        VerifiedAtUtc =
            verifiedAtUtc;

        ExpiredAtUtc =
            null;

        ReviewNote =
            normalizedReviewNote;

        Touch(
            verifiedAtUtc,
            reviewedByUserId);
    }

    public void MarkExpired(
        DateTimeOffset expiredAtUtc,
        Guid? actorUserId = null)
    {
        if (Status !=
            MerchantVerificationStatus.Verified)
        {
            throw new InvalidOperationException(
                "Only a verified merchant verification profile can expire.");
        }

        Status =
            MerchantVerificationStatus.Expired;

        ExpiredAtUtc =
            expiredAtUtc;

        ReviewNote =
            null;

        Touch(
            expiredAtUtc,
            actorUserId);
    }

    private void EnsureEditable()
    {
        if (Status is
            MerchantVerificationStatus.Submitted or
            MerchantVerificationStatus.UnderReview or
            MerchantVerificationStatus.Verified)
        {
            throw new InvalidOperationException(
                "Merchant verification details cannot be edited while submitted, under review, or verified.");
        }
    }

    private void EnsureReviewable(
        Guid reviewedByUserId)
    {
        if (reviewedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Reviewer user ID cannot be empty.",
                nameof(reviewedByUserId));
        }

        if (Status is not
            (MerchantVerificationStatus.Submitted
            or MerchantVerificationStatus.UnderReview))
        {
            throw new InvalidOperationException(
                "The merchant verification profile cannot be reviewed from its current state.");
        }
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

    private static void ValidatePrincipalUserId(
        UserId principalUserId)
    {
        if (principalUserId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Principal user ID cannot be empty.",
                nameof(principalUserId));
        }
    }

    private static void ValidateSubjectType(
        MerchantVerificationSubjectType subjectType)
    {
        if (subjectType ==
                MerchantVerificationSubjectType.Unknown ||
            !Enum.IsDefined(
                subjectType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(subjectType),
                "A supported merchant verification subject type is required.");
        }
    }

    private static string NormalizeCountryCode(
        string countryCode)
    {
        if (string.IsNullOrWhiteSpace(
                countryCode))
        {
            throw new ArgumentException(
                "Country code is required.",
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
                "Country code must be a two-letter ISO country code.",
                nameof(countryCode));
        }

        return normalized;
    }

    private static string NormalizeLegalName(
        string legalName)
    {
        if (string.IsNullOrWhiteSpace(
                legalName))
        {
            throw new ArgumentException(
                "Legal name is required.",
                nameof(legalName));
        }

        var normalized =
            legalName.Trim();

        if (normalized.Length >
            MaxLegalNameLength)
        {
            throw new ArgumentException(
                $"Legal name cannot exceed {MaxLegalNameLength} characters.",
                nameof(legalName));
        }

        return normalized;
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
}