using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Commerce.Verification.Review;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tests.Commerce.Verification.Review;

public sealed class RequestMoreInformationMerchantVerificationReviewHandlerTests
{
    [Fact]
    public async Task HandleAsync_SubmittedProfile_RequestsMoreInformation()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                14,
                13,
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

        var result =
            await handler.HandleAsync(
                new RequestMoreInformationMerchantVerificationReviewCommand(
                    profile.Id,
                    reviewerUserId,
                    "  Please upload a clearer identity document.  "));

        Assert.Equal(
            MerchantVerificationStatus.RequiresMoreInformation,
            profile.Status);

        Assert.Equal(
            MerchantVerificationStatus.RequiresMoreInformation,
            result.Status);

        Assert.Equal(
            "Please upload a clearer identity document.",
            profile.ReviewNote);

        Assert.Equal(
            profile.ReviewNote,
            result.ReviewNote);

        Assert.Equal(
            reviewerUserId.Value,
            profile.ReviewedByUserId);

        Assert.Equal(
            now,
            profile.ReviewedAtUtc);

        Assert.Equal(
            1,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_UnderReviewBySameReviewer_RequestsMoreInformation()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                14,
                13,
                5,
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

        profile.StartReview(
            now.AddMinutes(-15),
            reviewerUserId.Value);

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
            new RequestMoreInformationMerchantVerificationReviewCommand(
                profile.Id,
                reviewerUserId,
                "Additional business evidence is required."));

        Assert.Equal(
            MerchantVerificationStatus.RequiresMoreInformation,
            profile.Status);

        Assert.Equal(
            reviewerUserId.Value,
            profile.ReviewedByUserId);

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
            CreateSubmittedProfile(
                principalUserId,
                now.AddHours(-1));

        profile.StartReview(
            now.AddMinutes(-15),
            assignedReviewerUserId.Value);

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
                        new RequestMoreInformationMerchantVerificationReviewCommand(
                            profile.Id,
                            otherReviewerUserId,
                            "More information is required.")));

        Assert.Equal(
            MerchantVerificationStatus.UnderReview,
            profile.Status);

        Assert.Equal(
            assignedReviewerUserId.Value,
            profile.ReviewedByUserId);

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
                        new RequestMoreInformationMerchantVerificationReviewCommand(
                            profile.Id,
                            principalUserId,
                            "More information is required.")));

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
                        new RequestMoreInformationMerchantVerificationReviewCommand(
                            profile.Id,
                            reviewerUserId,
                            "More information is required.")));

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_WhitespaceReviewNote_DoesNotSaveOrMutate()
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

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                handler.HandleAsync(
                    new RequestMoreInformationMerchantVerificationReviewCommand(
                        profile.Id,
                        reviewerUserId,
                        "   ")));

        Assert.Equal(
            MerchantVerificationStatus.Submitted,
            profile.Status);

        Assert.Null(
            profile.ReviewNote);

        Assert.Null(
            profile.ReviewedAtUtc);

        Assert.Null(
            profile.ReviewedByUserId);

        Assert.Equal(
            0,
            repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_InvalidProfileState_IsRejected()
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
                "Verification Owner",
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

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                handler.HandleAsync(
                    new RequestMoreInformationMerchantVerificationReviewCommand(
                        profile.Id,
                        reviewerUserId,
                        "More information is required.")));

        Assert.Equal(
            MerchantVerificationStatus.Draft,
            profile.Status);

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
                    new RequestMoreInformationMerchantVerificationReviewCommand(
                        MerchantVerificationProfileId.New(),
                        UserId.New(),
                        "More information is required.")));

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
                        new RequestMoreInformationMerchantVerificationReviewCommand(
                            profile.Id,
                            reviewerUserId,
                            "Additional documentation is required.")));

        Assert.Same(
            concurrencyException,
            thrownException);

        Assert.Equal(
            1,
            repository.SaveChangesCallCount);
    }

    private static RequestMoreInformationMerchantVerificationReviewHandler
        CreateHandler(
            FakeReviewRepository repository,
            DateTimeOffset now)
    {
        return new RequestMoreInformationMerchantVerificationReviewHandler(
            repository,
            new MerchantVerificationReviewConflictGuard(
                repository),
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