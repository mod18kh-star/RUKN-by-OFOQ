using OFOQ.Market.Domain.Commerce.Verification;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IMerchantVerificationDocumentRepository
{
    Task<IReadOnlyList<MerchantVerificationDocument>> GetByProfileAsync(
        MerchantVerificationProfileId profileId,
        CancellationToken cancellationToken = default);

    Task<MerchantVerificationDocument?> GetByIdAsync(
        MerchantVerificationDocumentId documentId,
        CancellationToken cancellationToken = default);

    Task<MerchantVerificationDocument?> GetByFingerprintAsync(
        MerchantVerificationProfileId profileId,
        string documentNumberFingerprint,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MerchantVerificationDocument document,
        CancellationToken cancellationToken = default);
}
