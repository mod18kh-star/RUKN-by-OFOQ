using OFOQ.Market.Application.Common.Files;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Verification;

namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed record MerchantVerificationReviewFileDownload(
    string OriginalFileName,
    string ContentType,
    Stream Content);

public sealed class OpenMerchantVerificationReviewFileHandler
{
    private readonly IMerchantVerificationReviewQueryRepository
        _repository;

    private readonly IMerchantVerificationPrivateFileStore
        _fileStore;

    public OpenMerchantVerificationReviewFileHandler(
        IMerchantVerificationReviewQueryRepository repository,
        IMerchantVerificationPrivateFileStore fileStore)
    {
        _repository =
            repository;

        _fileStore =
            fileStore;
    }

    public async Task<MerchantVerificationReviewFileDownload?>
        HandleAsync(
            MerchantVerificationProfileId profileId,
            MerchantVerificationDocumentFileId fileId,
            CancellationToken cancellationToken = default)
    {
        var profile =
            await _repository.GetProfileByIdAsync(
                profileId,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var files =
            await _repository.ListDocumentFilesAsync(
                profileId,
                cancellationToken);

        var file =
            files.SingleOrDefault(
                candidate =>
                    candidate.Id ==
                    fileId);

        if (file is null)
        {
            return null;
        }

        var stream =
            await _fileStore.OpenReadAsync(
                file.TenantId,
                file.StorageKey,
                cancellationToken);

        return new MerchantVerificationReviewFileDownload(
            file.OriginalFileName,
            file.ContentType,
            stream);
    }
}