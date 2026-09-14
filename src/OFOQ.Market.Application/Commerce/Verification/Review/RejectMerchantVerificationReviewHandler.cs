using OFOQ.Market.Application.Common.Persistence;

namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed class RejectMerchantVerificationReviewHandler
{
    private readonly IMerchantVerificationReviewRepository
        _reviewRepository;

    private readonly MerchantVerificationReviewConflictGuard
        _conflictGuard;

    private readonly TimeProvider
        _timeProvider;

    public RejectMerchantVerificationReviewHandler(
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

    public async Task<RejectMerchantVerificationReviewResult>
        HandleAsync(
            RejectMerchantVerificationReviewCommand command,
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
         * If the profile is already UnderReview, only the
         * reviewer who claimed it may complete the action.
         */
        await _conflictGuard
            .EnsureReviewerCanActAsync(
                profile,
                command.ReviewerUserId,
                cancellationToken);

        var now =
            _timeProvider.GetUtcNow();

        /*
         * The domain remains authoritative for:
         * - allowed review states
         * - required review note
         * - review-note normalization
         * - final Rejected state
         */
        profile.Reject(
            command.ReviewNote,
            now,
            command.ReviewerUserId.Value);

        /*
         * Optimistic concurrency is enforced by the repository
         * through PostgreSQL xmin.
         */
        await _reviewRepository
            .SaveChangesAsync(
                cancellationToken);

        return new RejectMerchantVerificationReviewResult(
            profile.Id,
            profile.TenantId,
            profile.Status,
            now,
            command.ReviewerUserId,
            profile.ReviewNote!);
    }
}