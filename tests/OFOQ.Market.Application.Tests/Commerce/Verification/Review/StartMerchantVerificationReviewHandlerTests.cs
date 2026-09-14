using OFOQ.Market.Application.Commerce.Verification.Review;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tests.Commerce.Verification.Review;

public sealed class StartMerchantVerificationReviewHandlerTests
{
    [Fact]
    public async Task HandleAsync_SubmittedProfileWithIndependentReviewer_StartsReview()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                12,
                16,
                0,
                0,
                TimeSpan.Zero);

        var principalUserId =
            UserId.New();

        var reviewerUserId =
            UserId.New();

        var profile =
            CreateSubmittedProfile(
                principalUserId,
                now.AddHours(-1));

        var repository =
            new FakeReviewRepository
            {
                Profile = profile
            };

        var handler =
            CreateHandler(
                repository,
                now);

        await handler.HandleAsync(
            new StartMerchantVerificationReviewCommand(
                profile.Id,
                reviewerUserId));

        Assert.Equal(
            MerchantVerificationStatus.UnderReview,
            profile.Status);

        Assert.Equal(
            now,
            profile.ReviewStartedAtUtc);

        Assert.Equal(
            reviewerUserId.Value,
            profile.ReviewedByUserId);

        Assert.Equal(
            reviewerUserId.Value,
            profile.UpdatedByUserId);

        Assert.Equal(
            1,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenReviewerIsPrincipal_RejectsConflictWithoutSaving()
    {
        var now =
            DateTimeOffset.UtcNow;

        var principalUserId =
            UserId.New();

        var profile =
            CreateSubmittedProfile(
                principalUserId,
                now.AddHours(-1));

        var repository =
            new FakeReviewRepository
            {
                Profile = profile
            };

        var handler =
            CreateHandler(
                repository,
                now);

        await Assert.ThrowsAsync<
            MerchantVerificationReviewConflictException>(
                () =>
                    handler.HandleAsync(
                        new StartMerchantVerificationReviewCommand(
                            profile.Id,
                            principalUserId)));

        Assert.Equal(
            MerchantVerificationStatus.Submitted,
            profile.Status);

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenReviewerHasActiveTenantMembership_RejectsConflictWithoutSaving()
    {
        var now =
            DateTimeOffset.UtcNow;

        var principalUserId =
            UserId.New();

        var reviewerUserId =
            UserId.New();

        var profile =
            CreateSubmittedProfile(
                principalUserId,
                now.AddHours(-1));

        var repository =
            new FakeReviewRepository
            {
                Profile = profile,
                HasActiveMembership = true
            };

        var handler =
            CreateHandler(
                repository,
                now);

        await Assert.ThrowsAsync<
            MerchantVerificationReviewConflictException>(
                () =>
                    handler.HandleAsync(
                        new StartMerchantVerificationReviewCommand(
                            profile.Id,
                            reviewerUserId)));

        Assert.Equal(
            MerchantVerificationStatus.Submitted,
            profile.Status);

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenProfileDoesNotExist_ThrowsWithoutSaving()
    {
        var repository =
            new FakeReviewRepository();

        var handler =
            CreateHandler(
                repository,
                DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<
            KeyNotFoundException>(
                () =>
                    handler.HandleAsync(
                        new StartMerchantVerificationReviewCommand(
                            MerchantVerificationProfileId.New(),
                            UserId.New())));

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenProfileIsNotSubmitted_DomainRejectsTransitionWithoutSaving()
    {
        var now =
            DateTimeOffset.UtcNow;

        var principalUserId =
            UserId.New();

        var reviewerUserId =
            UserId.New();

        var profile =
            MerchantVerificationProfile.Create(
                TenantId.New(),
                principalUserId,
                MerchantVerificationSubjectType.Individual,
                "SA",
                "Draft Verification Owner",
                now.AddHours(-1),
                principalUserId.Value);

        var repository =
            new FakeReviewRepository
            {
                Profile = profile
            };

        var handler =
            CreateHandler(
                repository,
                now);

        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () =>
                    handler.HandleAsync(
                        new StartMerchantVerificationReviewCommand(
                            profile.Id,
                            reviewerUserId)));

        Assert.Equal(
            MerchantVerificationStatus.Draft,
            profile.Status);

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenConcurrentUpdateOccurs_PropagatesConcurrencyConflict()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                14,
                12,
                45,
                0,
                TimeSpan.Zero);

        var principalUserId =
            UserId.New();

        var reviewerUserId =
            UserId.New();

        var profile =
            CreateSubmittedProfile(
                principalUserId,
                now.AddHours(-1));

        var concurrencyException =
            new MerchantVerificationReviewConcurrencyException();

        var repository =
            new FakeReviewRepository
            {
                Profile = profile,
                SaveChangesException =
                    concurrencyException
            };

        var handler =
            CreateHandler(
                repository,
                now);

        var thrownException =
            await Assert.ThrowsAsync<
                MerchantVerificationReviewConcurrencyException>(
                () =>
                    handler.HandleAsync(
                        new StartMerchantVerificationReviewCommand(
                            profile.Id,
                            reviewerUserId)));

        Assert.Same(
            concurrencyException,
            thrownException);

        Assert.Equal(
            1,
            repository.SaveChangesCallCount);
    }
    private static StartMerchantVerificationReviewHandler
        CreateHandler(
            FakeReviewRepository repository,
            DateTimeOffset now)
    {
        var conflictGuard =
            new MerchantVerificationReviewConflictGuard(
                repository);

        return new StartMerchantVerificationReviewHandler(
            repository,
            conflictGuard,
            new FixedTimeProvider(
                now));
    }

    private static MerchantVerificationProfile
        CreateSubmittedProfile(
            UserId principalUserId,
            DateTimeOffset createdAtUtc)
    {
        var profile =
            MerchantVerificationProfile.Create(
                TenantId.New(),
                principalUserId,
                MerchantVerificationSubjectType.Individual,
                "SA",
                "Verification Owner",
                createdAtUtc,
                principalUserId.Value);

        profile.Submit(
            createdAtUtc.AddMinutes(5),
            principalUserId.Value);

        return profile;
    }

    private sealed class FakeReviewRepository :
        IMerchantVerificationReviewRepository
    {
        public MerchantVerificationProfile?
            Profile { get; init; }

        public bool HasActiveMembership { get; init; }

        public Exception?
            SaveChangesException { get; init; }

        public int SaveChangesCallCount { get; private set; }

        public Task<MerchantVerificationProfile?>
            GetProfileByIdAsync(
                MerchantVerificationProfileId profileId,
                CancellationToken cancellationToken = default)
        {
            if (Profile is null ||
                Profile.Id != profileId)
            {
                return Task.FromResult<
                    MerchantVerificationProfile?>(
                        null);
            }

            return Task.FromResult<
                MerchantVerificationProfile?>(
                    Profile);
        }

        public Task<bool>
            HasActiveTenantMembershipAsync(
                TenantId tenantId,
                UserId userId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                HasActiveMembership);
        }

        public Task<int>
            SaveChangesAsync(
                CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            if (SaveChangesException is not null)
            {
                throw SaveChangesException;
            }

            return Task.FromResult(
                1);
        }
    }

    private sealed class FixedTimeProvider :
        TimeProvider
    {
        private readonly DateTimeOffset
            _utcNow;

        public FixedTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow =
                utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
