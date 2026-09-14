using OFOQ.Market.Application.Common.Persistence;

namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed class StartMerchantVerificationReviewHandler
{
    private readonly IMerchantVerificationReviewRepository
        _reviewRepository;

    private readonly MerchantVerificationReviewConflictGuard
        _conflictGuard;

    private readonly TimeProvider
        _timeProvider;

    public StartMerchantVerificationReviewHandler(
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

    public async Task<StartMerchantVerificationReviewResult>
        HandleAsync(
            StartMerchantVerificationReviewCommand command,
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
         * Independence is enforced centrally so every
         * KYC review action uses exactly the same rule.
         */
        await _conflictGuard
            .EnsureReviewerIsIndependentAsync(
                profile,
                command.ReviewerUserId,
                cancellationToken);

        var now =
            _timeProvider.GetUtcNow();

        /*
         * Domain rules remain authoritative.
         *
         * MerchantVerificationProfile.StartReview rejects
         * every state except Submitted.
         */
        profile.StartReview(
            now,
            command.ReviewerUserId.Value);

        /*
         * The privileged repository writes only through
         * its isolated tenant-scoped review context.
         */
        await _reviewRepository
            .SaveChangesAsync(
                cancellationToken);

        return new StartMerchantVerificationReviewResult(
            profile.Id,
            profile.TenantId,
            profile.Status,
            now,
            command.ReviewerUserId);
    }
}
