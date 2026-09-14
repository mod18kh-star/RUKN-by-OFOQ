using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Commerce.Verification.Review;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tests.Commerce.Verification.Review;

public sealed class VerifyMerchantVerificationReviewHandlerTests
{
    [Fact]
    public async Task HandleAsync_UnderReviewBySameReviewer_VerifiesProfile()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                14,
                17,
                30,
                0,
                TimeSpan.Zero);

        var principalUserId =
            UserId.New();

        var reviewerUserId =
            UserId.New();

        var profile =
            CreateUnderReviewProfile(
                principalUserId,
                reviewerUserId,
                now);

        var repository =
            new FakeReviewRepository
            {
                Profile = profile
            };

        var handler =
            CreateHandler(
                repository,
                now);

        var result =
            await handler.HandleAsync(
                new VerifyMerchantVerificationReviewCommand(
                    profile.Id,
                    reviewerUserId,
                    "  Identity and submitted evidence validated.  "));

        Assert.Equal(
            MerchantVerificationStatus.Verified,
            profile.Status);

        Assert.Equal(
            MerchantVerificationStatus.Verified,
            result.Status);

        Assert.Equal(
            now,
            profile.VerifiedAtUtc);

        Assert.Equal(
            now,
            result.VerifiedAtUtc);

        Assert.Equal(
            now,
            profile.ReviewedAtUtc);

        Assert.Equal(
            reviewerUserId.Value,
            profile.ReviewedByUserId);

        Assert.Equal(
            "Identity and submitted evidence validated.",
            profile.ReviewNote);

        Assert.Equal(
            profile.ReviewNote,
            result.ReviewNote);

        Assert.Equal(
            1,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhitespaceOptionalNote_VerifiesWithNullNote()
    {
        var now =
            DateTimeOffset.UtcNow;

        var principalUserId =
            UserId.New();

        var reviewerUserId =
            UserId.New();

        var profile =
            CreateUnderReviewProfile(
                principalUserId,
                reviewerUserId,
                now);

        var repository =
            new FakeReviewRepository
            {
                Profile = profile
            };

        var handler =
            CreateHandler(
                repository,
                now);

        var result =
            await handler.HandleAsync(
                new VerifyMerchantVerificationReviewCommand(
                    profile.Id,
                    reviewerUserId,
                    "   "));

        Assert.Equal(
            MerchantVerificationStatus.Verified,
            profile.Status);

        Assert.Null(
            profile.ReviewNote);

        Assert.Null(
            result.ReviewNote);

        Assert.Equal(
            1,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_UnderReviewByDifferentReviewer_IsRejected()
    {
        var now =
            DateTimeOffset.UtcNow;

        var principalUserId =
            UserId.New();

        var assignedReviewerUserId =
            UserId.New();

        var otherReviewerUserId =
            UserId.New();

        var profile =
            CreateUnderReviewProfile(
                principalUserId,
                assignedReviewerUserId,
                now);

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
                        new VerifyMerchantVerificationReviewCommand(
                            profile.Id,
                            otherReviewerUserId)));

        Assert.Equal(
            MerchantVerificationStatus.UnderReview,
            profile.Status);

        Assert.Null(
            profile.VerifiedAtUtc);

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_PrincipalReviewer_IsRejected()
    {
        var now =
            DateTimeOffset.UtcNow;

        var principalUserId =
            UserId.New();

        var profile =
            CreateSubmittedProfile(
                principalUserId,
                now.AddHours(-1));

        /*
         * Domain methods themselves do not perform platform
         * conflict-of-interest authorization. This intentionally
         * constructs the state so the application guard is tested.
         */
        profile.StartReview(
            now.AddMinutes(-15),
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
            MerchantVerificationReviewConflictException>(
                () =>
                    handler.HandleAsync(
                        new VerifyMerchantVerificationReviewCommand(
                            profile.Id,
                            principalUserId)));

        Assert.Equal(
            MerchantVerificationStatus.UnderReview,
            profile.Status);

        Assert.Null(
            profile.VerifiedAtUtc);

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ReviewerHasActiveTenantMembership_IsRejected()
    {
        var now =
            DateTimeOffset.UtcNow;

        var principalUserId =
            UserId.New();

        var reviewerUserId =
            UserId.New();

        var profile =
            CreateUnderReviewProfile(
                principalUserId,
                reviewerUserId,
                now);

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
                        new VerifyMerchantVerificationReviewCommand(
                            profile.Id,
                            reviewerUserId)));

        Assert.Equal(
            MerchantVerificationStatus.UnderReview,
            profile.Status);

        Assert.Null(
            profile.VerifiedAtUtc);

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_SubmittedProfile_IsRejected()
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
                Profile = profile
            };

        var handler =
            CreateHandler(
                repository,
                now);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                handler.HandleAsync(
                    new VerifyMerchantVerificationReviewCommand(
                        profile.Id,
                        reviewerUserId)));

        Assert.Equal(
            MerchantVerificationStatus.Submitted,
            profile.Status);

        Assert.Null(
            profile.VerifiedAtUtc);

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ReviewNoteTooLong_DoesNotSaveOrMutate()
    {
        var now =
            DateTimeOffset.UtcNow;

        var principalUserId =
            UserId.New();

        var reviewerUserId =
            UserId.New();

        var profile =
            CreateUnderReviewProfile(
                principalUserId,
                reviewerUserId,
                now);

        var repository =
            new FakeReviewRepository
            {
                Profile = profile
            };

        var handler =
            CreateHandler(
                repository,
                now);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                handler.HandleAsync(
                    new VerifyMerchantVerificationReviewCommand(
                        profile.Id,
                        reviewerUserId,
                        new string(
                            'x',
                            MerchantVerificationProfile.MaxReviewNoteLength + 1))));

        Assert.Equal(
            MerchantVerificationStatus.UnderReview,
            profile.Status);

        Assert.Null(
            profile.VerifiedAtUtc);

        Assert.Null(
            profile.ReviewNote);

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ProfileNotFound_IsRejected()
    {
        var repository =
            new FakeReviewRepository();

        var handler =
            CreateHandler(
                repository,
                DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () =>
                handler.HandleAsync(
                    new VerifyMerchantVerificationReviewCommand(
                        MerchantVerificationProfileId.New(),
                        UserId.New())));

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_ConcurrentUpdate_PropagatesConcurrencyConflict()
    {
        var now =
            DateTimeOffset.UtcNow;

        var principalUserId =
            UserId.New();

        var reviewerUserId =
            UserId.New();

        var profile =
            CreateUnderReviewProfile(
                principalUserId,
                reviewerUserId,
                now);

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
                        new VerifyMerchantVerificationReviewCommand(
                            profile.Id,
                            reviewerUserId)));

        Assert.Same(
            concurrencyException,
            thrownException);

        Assert.Equal(
            1,
            repository.SaveChangesCallCount);
    }

    private static VerifyMerchantVerificationReviewHandler
        CreateHandler(
            FakeReviewRepository repository,
            DateTimeOffset now)
    {
        return new VerifyMerchantVerificationReviewHandler(
            repository,
            new MerchantVerificationReviewConflictGuard(
                repository),
            new FixedTimeProvider(
                now));
    }

    private static MerchantVerificationProfile
        CreateUnderReviewProfile(
            UserId principalUserId,
            UserId reviewerUserId,
            DateTimeOffset now)
    {
        var profile =
            CreateSubmittedProfile(
                principalUserId,
                now.AddHours(-1));

        profile.StartReview(
            now.AddMinutes(-15),
            reviewerUserId.Value);

        return profile;
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

    private sealed class FakeReviewRepository :
        IMerchantVerificationReviewRepository
    {
        public MerchantVerificationProfile?
            Profile { get; init; }

        public bool
            HasActiveMembership { get; init; }

        public Exception?
            SaveChangesException { get; init; }

        public int
            SaveChangesCallCount { get; private set; }

        public Task<MerchantVerificationProfile?>
            GetProfileByIdAsync(
                MerchantVerificationProfileId profileId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Profile is not null &&
                Profile.Id == profileId
                    ? Profile
                    : null);
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
}