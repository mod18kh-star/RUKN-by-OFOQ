using OFOQ.Market.Domain.Commerce.Verification;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IMerchantVerificationReviewQueryRepository
{
    Task<IReadOnlyList<MerchantVerificationProfile>>
        ListProfilesAsync(
            MerchantVerificationStatus? status,
            int take,
            CancellationToken cancellationToken = default);

    Task<MerchantVerificationProfile?>
        GetProfileByIdAsync(
            MerchantVerificationProfileId profileId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MerchantVerificationDocument>>
        ListDocumentsAsync(
            MerchantVerificationProfileId profileId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MerchantVerificationDocumentFile>>
        ListDocumentFilesAsync(
            MerchantVerificationProfileId profileId,
            CancellationToken cancellationToken = default);
}