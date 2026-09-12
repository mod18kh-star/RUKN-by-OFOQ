using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Files;

public sealed record MerchantVerificationPrivateFileUpload(
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    Stream Content);

public sealed record MerchantVerificationPrivateFileResult(
    string StorageKey,
    string Sha256,
    long FileSizeBytes,
    string ContentType);

public interface IMerchantVerificationPrivateFileStore
{
    Task<MerchantVerificationPrivateFileResult> StoreAsync(
        TenantId tenantId,
        MerchantVerificationProfileId profileId,
        MerchantVerificationDocumentId documentId,
        MerchantVerificationDocumentSide side,
        MerchantVerificationPrivateFileUpload file,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        TenantId tenantId,
        string storageKey,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        TenantId tenantId,
        string storageKey,
        CancellationToken cancellationToken = default);
}