using OFOQ.Market.Application.Common.Persistence;

namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed class VerifyMerchantVerificationReviewHandler
{
    private readonly IMerchantVerificationReviewRepository
        _reviewRepository;

    private readonly MerchantVerificationReviewConflictGuard
        _conflictGuard;

    private readonly TimeProvider
        _timeProvider;

    public VerifyMerchantVerificationReviewHandler(
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

    public async Task<VerifyMerchantVerificationReviewResult>
        HandleAsync(
            VerifyMerchantVerificationReviewCommand command,
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
         * Verification is a privileged review action.
         *
         * Conflict-of-interest rules always apply and an
         * UnderReview profile may only be completed by the
         * reviewer who previously claimed it.
         */
        await _conflictGuard
            .EnsureReviewerCanActAsync(
                profile,
                command.ReviewerUserId,
                cancellationToken);

        var now =
            _timeProvider.GetUtcNow();

        /*
         * Domain rules remain authoritative:
         *
         * - only UnderReview may become Verified
         * - review note is optional
         * - supplied note is normalized and validated
         * - VerifiedAtUtc is recorded atomically
         */
        profile.Verify(
            now,
            command.ReviewerUserId.Value,
            command.ReviewNote);

        /*
         * PostgreSQL xmin protects against a stale reviewer
         * overwriting another concurrent review operation.
         */
        await _reviewRepository
            .SaveChangesAsync(
                cancellationToken);

        return new VerifyMerchantVerificationReviewResult(
            profile.Id,
            profile.TenantId,
            profile.Status,
            profile.VerifiedAtUtc!.Value,
            command.ReviewerUserId,
            profile.ReviewNote);
    }
}