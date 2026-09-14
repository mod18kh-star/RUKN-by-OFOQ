using OFOQ.Market.Application.Common.Persistence;

namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed class RequestMoreInformationMerchantVerificationReviewHandler
{
    private readonly IMerchantVerificationReviewRepository
        _reviewRepository;

    private readonly MerchantVerificationReviewConflictGuard
        _conflictGuard;

    private readonly TimeProvider
        _timeProvider;

    public RequestMoreInformationMerchantVerificationReviewHandler(
        IMerchantVerificationReviewRepository reviewRepository,
        MerchantVerificationReviewConflictGuard conflictGuard,
        TimeProvider timeProvider)
    {
        _reviewRepository =
            reviewRepository;

        _conflictGuard =
            conflictGuard;

        _timeProvider =
            timeProvider;
    }

    public async Task<RequestMoreInformationMerchantVerificationReviewResult>
        HandleAsync(
            RequestMoreInformationMerchantVerificationReviewCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (command.ProfileId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Merchant verification profile ID cannot be empty.",
                nameof(command));
        }

        if (command.ReviewerUserId.IsEmpty)
        {
            throw new ArgumentException(
                "Reviewer user ID cannot be empty.",
                nameof(command));
        }

        var profile =
            await _reviewRepository
                .GetProfileByIdAsync(
                    command.ProfileId,
                    cancellationToken);

        if (profile is null)
        {
            throw new KeyNotFoundException(
                "Merchant verification profile was not found.");
        }

        /*
         * Conflict-of-interest rules always apply.
         *
         * If the profile is already UnderReview, the same
         * reviewer who claimed it must perform the action.
         */
        await _conflictGuard
            .EnsureReviewerCanActAsync(
                profile,
                command.ReviewerUserId,
                cancellationToken);

        var now =
            _timeProvider.GetUtcNow();

        /*
         * The domain owns state-transition and review-note rules.
         *
         * It allows this action only from Submitted or UnderReview
         * and requires a non-empty review note.
         */
        profile.RequestMoreInformation(
            command.ReviewNote,
            now,
            command.ReviewerUserId.Value);

        await _reviewRepository
            .SaveChangesAsync(
                cancellationToken);

        return new RequestMoreInformationMerchantVerificationReviewResult(
            profile.Id,
            profile.TenantId,
            profile.Status,
            now,
            command.ReviewerUserId,
            profile.ReviewNote!);
    }
}