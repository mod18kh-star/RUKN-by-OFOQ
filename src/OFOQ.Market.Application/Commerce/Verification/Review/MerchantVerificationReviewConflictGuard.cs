using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed class MerchantVerificationReviewConflictGuard
{
    private readonly IMerchantVerificationReviewRepository
        _reviewRepository;

    public MerchantVerificationReviewConflictGuard(
        IMerchantVerificationReviewRepository reviewRepository)
    {
        _reviewRepository =
            reviewRepository;
    }

    public async Task EnsureReviewerIsIndependentAsync(
        MerchantVerificationProfile profile,
        UserId reviewerUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        if (reviewerUserId.IsEmpty)
        {
            throw new ArgumentException(
                "Reviewer user ID cannot be empty.",
                nameof(reviewerUserId));
        }

        /*
         * The legal principal of the verification profile
         * can never review their own verification case.
         */
        if (profile.PrincipalUserId ==
            reviewerUserId)
        {
            throw new MerchantVerificationReviewConflictException();
        }

        /*
         * Any active tenant membership creates a conflict
         * of interest, regardless of tenant role.
         *
         * PlatformAdministrator is intentionally not exempt.
         */
        var hasTenantMembership =
            await _reviewRepository
                .HasActiveTenantMembershipAsync(
                    profile.TenantId,
                    reviewerUserId,
                    cancellationToken);

        if (hasTenantMembership)
        {
            throw new MerchantVerificationReviewConflictException();
        }
    }
}
