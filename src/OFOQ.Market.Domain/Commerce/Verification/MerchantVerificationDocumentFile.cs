using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Verification;

public sealed class MerchantVerificationDocumentFile :
    AggregateRoot<MerchantVerificationDocumentFileId>,
    ITenantDataScoped,
    IAuditable
{
    public const int MaxStorageKeyLength =
        500;

    public const int MaxOriginalFileNameLength =
        255;

    public const int MaxContentTypeLength =
        100;

    public const int MaxSha256Length =
        64;

    public const long MaxFileSizeBytes =
        15L * 1024L * 1024L;

    private MerchantVerificationDocumentFile()
    {
    }

    private MerchantVerificationDocumentFile(
        MerchantVerificationDocumentFileId id,
        TenantId tenantId,
        MerchantVerificationDocumentId documentId,
        MerchantVerificationDocumentSide side,
        string storageKey,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string sha256,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        ValidateTenantId(
            tenantId);

        ValidateDocumentId(
            documentId);

        ValidateSide(
            side);

        TenantId =
            tenantId;

        DocumentId =
            documentId;

        Side =
            side;

        StorageKey =
            NormalizeStorageKey(
                storageKey);

        OriginalFileName =
            NormalizeOriginalFileName(
                originalFileName);

        ContentType =
            NormalizeContentType(
                contentType);

        FileSizeBytes =
            ValidateFileSize(
                fileSizeBytes);

        Sha256 =
            NormalizeSha256(
                sha256);

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public MerchantVerificationDocumentId DocumentId { get; private set; }

    public MerchantVerificationDocumentSide Side { get; private set; }

    public string StorageKey { get; private set; } =
        string.Empty;

    public string OriginalFileName { get; private set; } =
        string.Empty;

    public string ContentType { get; private set; } =
        string.Empty;

    public long FileSizeBytes { get; private set; }

    public string Sha256 { get; private set; } =
        string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static MerchantVerificationDocumentFile Create(
        TenantId tenantId,
        MerchantVerificationDocumentId documentId,
        MerchantVerificationDocumentSide side,
        string storageKey,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        string sha256,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new MerchantVerificationDocumentFile(
            MerchantVerificationDocumentFileId.New(),
            tenantId,
            documentId,
            side,
            storageKey,
            originalFileName,
            contentType,
            fileSizeBytes,
            sha256,
            createdAtUtc,
            createdByUserId);
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

    private static void ValidateDocumentId(
        MerchantVerificationDocumentId documentId)
    {
        if (documentId.IsEmpty)
        {
            throw new ArgumentException(
                "Merchant verification document ID cannot be empty.",
                nameof(documentId));
        }
    }

    private static void ValidateSide(
        MerchantVerificationDocumentSide side)
    {
        if (side ==
                MerchantVerificationDocumentSide.Unknown ||
            !Enum.IsDefined(
                side))
        {
            throw new ArgumentOutOfRangeException(
                nameof(side),
                "A supported merchant verification document side is required.");
        }
    }

    private static string NormalizeStorageKey(
        string storageKey)
    {
        if (string.IsNullOrWhiteSpace(
                storageKey))
        {
            throw new ArgumentException(
                "Private storage key is required.",
                nameof(storageKey));
        }

        var normalized =
            storageKey.Trim();

        if (normalized.Length >
            MaxStorageKeyLength)
        {
            throw new ArgumentException(
                $"Private storage key cannot exceed {MaxStorageKeyLength} characters.",
                nameof(storageKey));
        }

        return normalized;
    }

    private static string NormalizeOriginalFileName(
        string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(
                originalFileName))
        {
            throw new ArgumentException(
                "Original file name is required.",
                nameof(originalFileName));
        }

        var normalized =
            originalFileName.Trim();

        if (normalized.Length >
            MaxOriginalFileNameLength)
        {
            throw new ArgumentException(
                $"Original file name cannot exceed {MaxOriginalFileNameLength} characters.",
                nameof(originalFileName));
        }

        return normalized;
    }

    private static string NormalizeContentType(
        string contentType)
    {
        if (string.IsNullOrWhiteSpace(
                contentType))
        {
            throw new ArgumentException(
                "Content type is required.",
                nameof(contentType));
        }

        var normalized =
            contentType
                .Trim()
                .ToLowerInvariant();

        if (normalized.Length >
            MaxContentTypeLength)
        {
            throw new ArgumentException(
                $"Content type cannot exceed {MaxContentTypeLength} characters.",
                nameof(contentType));
        }

        return normalized;
    }

    private static long ValidateFileSize(
        long fileSizeBytes)
    {
        if (fileSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fileSizeBytes),
                "Merchant verification document file cannot be empty.");
        }

        if (fileSizeBytes >
            MaxFileSizeBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fileSizeBytes),
                $"Merchant verification document file cannot exceed {MaxFileSizeBytes} bytes.");
        }

        return fileSizeBytes;
    }

    private static string NormalizeSha256(
        string sha256)
    {
        if (string.IsNullOrWhiteSpace(
                sha256))
        {
            throw new ArgumentException(
                "SHA-256 checksum is required.",
                nameof(sha256));
        }

        var normalized =
            sha256
                .Trim()
                .ToLowerInvariant();

        if (normalized.Length !=
                MaxSha256Length ||
            !normalized.All(
                IsHexCharacter))
        {
            throw new ArgumentException(
                "SHA-256 checksum must be a 64-character hexadecimal value.",
                nameof(sha256));
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