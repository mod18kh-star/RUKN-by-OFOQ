using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Verification;

namespace OFOQ.Market.Application.Commerce.Verification.Review;

public sealed class GetMerchantVerificationReviewQueueHandler
{
    public const int DefaultTake =
        50;

    public const int MaxTake =
        200;

    private readonly IMerchantVerificationReviewQueryRepository
        _repository;

    public GetMerchantVerificationReviewQueueHandler(
        IMerchantVerificationReviewQueryRepository repository)
    {
        _repository =
            repository;
    }

    public async Task<IReadOnlyList<MerchantVerificationReviewSummaryResult>>
        HandleAsync(
            MerchantVerificationStatus? status = null,
            int take = DefaultTake,
            CancellationToken cancellationToken = default)
    {
        if (status ==
            MerchantVerificationStatus.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A supported merchant verification status is required.");
        }

        if (take <= 0 ||
            take > MaxTake)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take),
                $"Take must be between 1 and {MaxTake}.");
        }

        var profiles =
            await _repository
                .ListProfilesAsync(
                    status,
                    take,
                    cancellationToken);

        return profiles
            .Select(
                Map)
            .ToArray();
    }

    internal static MerchantVerificationReviewSummaryResult Map(
        MerchantVerificationProfile profile)
    {
        return new MerchantVerificationReviewSummaryResult(
            profile.Id,
            profile.TenantId,
            profile.PrincipalUserId,
            profile.SubjectType,
            profile.CountryCode,
            profile.LegalName,
            profile.Status,
            profile.SubmittedAtUtc,
            profile.ReviewStartedAtUtc,
            profile.ReviewedAtUtc,
            profile.ReviewedByUserId,
            profile.VerifiedAtUtc,
            profile.ExpiredAtUtc,
            profile.ReviewNote,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc);
    }
}