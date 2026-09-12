using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Commerce.Verification;

public sealed class MerchantVerificationDocumentTests
{
    private const string ValidFingerprint =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private const string ValidSha256 =
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void CreateDocument_StoresOnlyProtectedNumberAndFingerprint()
    {
        var document =
            MerchantVerificationDocument.Create(
                TenantId.New(),
                MerchantVerificationProfileId.New(),
                MerchantVerificationDocumentType.NationalId,
                "sa",
                "Mohammed Ahmad",
                "v1.encrypted-document-number",
                ValidFingerprint,
                new DateOnly(
                    2025,
                    1,
                    1),
                new DateOnly(
                    2035,
                    1,
                    1),
                DateTimeOffset.UtcNow);

        Assert.Equal(
            "SA",
            document.IssuingCountryCode);

        Assert.Equal(
            "v1.encrypted-document-number",
            document.ProtectedDocumentNumber);

        Assert.Equal(
            ValidFingerprint,
            document.DocumentNumberFingerprint);

        Assert.Equal(
            MerchantVerificationDocumentReviewStatus.PendingReview,
            document.ReviewStatus);
    }

    [Fact]
    public void CreateDocument_InvalidFingerprint_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                MerchantVerificationDocument.Create(
                    TenantId.New(),
                    MerchantVerificationProfileId.New(),
                    MerchantVerificationDocumentType.Passport,
                    "AE",
                    "Test User",
                    "encrypted",
                    "not-a-valid-fingerprint",
                    null,
                    null,
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CreateDocument_ExpiryBeforeIssue_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                MerchantVerificationDocument.Create(
                    TenantId.New(),
                    MerchantVerificationProfileId.New(),
                    MerchantVerificationDocumentType.ResidencePermit,
                    "SA",
                    "Test User",
                    "encrypted",
                    ValidFingerprint,
                    new DateOnly(
                        2030,
                        1,
                        1),
                    new DateOnly(
                        2029,
                        1,
                        1),
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AcceptedDocument_CannotBeEdited()
    {
        var document =
            CreateDocument();

        document.Accept(
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(
            () =>
                document.UpdateDetails(
                    MerchantVerificationDocumentType.Passport,
                    "SA",
                    "Changed User",
                    "new-encrypted-value",
                    ValidFingerprint,
                    null,
                    null,
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RejectedDocument_CanBeUpdatedAndReturnsToPendingReview()
    {
        var document =
            CreateDocument();

        document.Reject(
            "The image is not readable.",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        document.UpdateDetails(
            MerchantVerificationDocumentType.NationalId,
            "SA",
            "Mohammed Ahmad",
            "v2.reprotected-number",
            ValidFingerprint,
            new DateOnly(
                2025,
                1,
                1),
            new DateOnly(
                2035,
                1,
                1),
            DateTimeOffset.UtcNow.AddMinutes(
                1));

        Assert.Equal(
            MerchantVerificationDocumentReviewStatus.PendingReview,
            document.ReviewStatus);

        Assert.Null(
            document.ReviewNote);

        Assert.Null(
            document.ReviewedAtUtc);
    }

    [Fact]
    public void AcceptedDocument_CanExpire()
    {
        var document =
            CreateDocument();

        document.Accept(
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        document.MarkExpired(
            DateTimeOffset.UtcNow.AddYears(
                1));

        Assert.Equal(
            MerchantVerificationDocumentReviewStatus.Expired,
            document.ReviewStatus);
    }

    [Fact]
    public void DocumentFile_StoresPrivateMetadata()
    {
        var file =
            MerchantVerificationDocumentFile.Create(
                TenantId.New(),
                MerchantVerificationDocumentId.New(),
                MerchantVerificationDocumentSide.Front,
                "merchant-verification/private/object-key",
                "identity-front.jpg",
                "image/jpeg",
                1024,
                ValidSha256,
                DateTimeOffset.UtcNow);

        Assert.Equal(
            MerchantVerificationDocumentSide.Front,
            file.Side);

        Assert.Equal(
            "merchant-verification/private/object-key",
            file.StorageKey);

        Assert.Equal(
            "image/jpeg",
            file.ContentType);

        Assert.Equal(
            1024,
            file.FileSizeBytes);
    }

    [Fact]
    public void DocumentFile_InvalidSha256_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                MerchantVerificationDocumentFile.Create(
                    TenantId.New(),
                    MerchantVerificationDocumentId.New(),
                    MerchantVerificationDocumentSide.Front,
                    "private/key",
                    "identity.jpg",
                    "image/jpeg",
                    1024,
                    "invalid",
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void DocumentFile_TooLarge_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                MerchantVerificationDocumentFile.Create(
                    TenantId.New(),
                    MerchantVerificationDocumentId.New(),
                    MerchantVerificationDocumentSide.Front,
                    "private/key",
                    "identity.jpg",
                    "image/jpeg",
                    MerchantVerificationDocumentFile.MaxFileSizeBytes +
                    1,
                    ValidSha256,
                    DateTimeOffset.UtcNow));
    }

    private static MerchantVerificationDocument CreateDocument()
    {
        return MerchantVerificationDocument.Create(
            TenantId.New(),
            MerchantVerificationProfileId.New(),
            MerchantVerificationDocumentType.NationalId,
            "SA",
            "Mohammed Ahmad",
            "v1.encrypted-document-number",
            ValidFingerprint,
            new DateOnly(
                2025,
                1,
                1),
            new DateOnly(
                2035,
                1,
                1),
            DateTimeOffset.UtcNow);
    }
}