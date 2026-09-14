using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Verification;

namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed class GetMerchantVerificationReviewDetailHandler
{
    private readonly IMerchantVerificationReviewQueryRepository
        _repository;

    public GetMerchantVerificationReviewDetailHandler(
        IMerchantVerificationReviewQueryRepository repository)
    {
        _repository =
            repository;
    }

    public async Task<MerchantVerificationReviewDetailResult?>
        HandleAsync(
            MerchantVerificationProfileId profileId,
            CancellationToken cancellationToken = default)
    {
        if (profileId.IsEmpty)
        {
            throw new ArgumentException(
                "Merchant verification profile ID cannot be empty.",
                nameof(profileId));
        }

        var profile =
            await _repository
                .GetProfileByIdAsync(
                    profileId,
                    cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var documents =
            await _repository
                .ListDocumentsAsync(
                    profileId,
                    cancellationToken);

        var files =
            await _repository
                .ListDocumentFilesAsync(
                    profileId,
                    cancellationToken);

        var documentResults =
            documents
                .Select(
                    document =>
                        new MerchantVerificationReviewDocumentResult(
                            document.Id,
                            document.DocumentType,
                            document.IssuingCountryCode,
                            document.HolderName,
                            document.IssueDate,
                            document.ExpiryDate,
                            document.ReviewStatus,
                            document.ReviewNote,
                            document.ReviewedAtUtc,
                            document.ReviewedByUserId,
                            files
                                .Where(
                                    file =>
                                        file.DocumentId ==
                                        document.Id)
                                .OrderBy(
                                    file =>
                                        file.Side)
                                .Select(
                                    file =>
                                        new MerchantVerificationReviewDocumentFileResult(
                                            file.Id,
                                            file.Side,
                                            file.OriginalFileName,
                                            file.ContentType,
                                            file.FileSizeBytes,
                                            file.CreatedAtUtc))
                                .ToArray()))
                .ToArray();

        return new MerchantVerificationReviewDetailResult(
            GetMerchantVerificationReviewQueueHandler.Map(
                profile),
            documentResults);
    }
}