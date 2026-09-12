using OFOQ.Market.Domain.Commerce.Verification;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IMerchantVerificationDocumentFileRepository
{
    Task<IReadOnlyList<MerchantVerificationDocumentFile>> GetByDocumentAsync(
        MerchantVerificationDocumentId documentId,
        CancellationToken cancellationToken = default);

    Task<MerchantVerificationDocumentFile?> GetByIdAsync(
        MerchantVerificationDocumentFileId fileId,
        CancellationToken cancellationToken = default);

    Task<MerchantVerificationDocumentFile?> GetByStorageKeyAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MerchantVerificationDocumentFile file,
        CancellationToken cancellationToken = default);

    void Remove(
        MerchantVerificationDocumentFile file);
}
