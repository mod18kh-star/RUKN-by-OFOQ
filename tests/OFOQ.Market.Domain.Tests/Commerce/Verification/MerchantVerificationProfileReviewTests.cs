using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Commerce.Verification;

public sealed class MerchantVerificationProfileReviewTests
{
    [Fact]
    public void RequestMoreInformation_WhitespaceNote_DoesNotMutateProfile()
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

        Assert.Throws<ArgumentException>(
            () =>
                profile.RequestMoreInformation(
                    "   ",
                    now,
                    reviewerUserId.Value));

        Assert.Equal(
            MerchantVerificationStatus.Submitted,
            profile.Status);

        Assert.Null(
            profile.ReviewedAtUtc);

        Assert.Null(
            profile.ReviewedByUserId);

        Assert.Null(
            profile.ReviewNote);
    }

    [Fact]
    public void Reject_WhitespaceNote_DoesNotMutateProfile()
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

        Assert.Throws<ArgumentException>(
            () =>
                profile.Reject(
                    "   ",
                    now,
                    reviewerUserId.Value));

        Assert.Equal(
            MerchantVerificationStatus.Submitted,
            profile.Status);

        Assert.Null(
            profile.ReviewedAtUtc);

        Assert.Null(
            profile.ReviewedByUserId);

        Assert.Null(
            profile.ReviewNote);
    }

    [Fact]
    public void Verify_ReviewNoteTooLong_DoesNotMutateProfile()
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

        var reviewStartedAtUtc =
            now.AddMinutes(-10);

        profile.StartReview(
            reviewStartedAtUtc,
            reviewerUserId.Value);

        Assert.Throws<ArgumentException>(
            () =>
                profile.Verify(
                    now,
                    reviewerUserId.Value,
                    new string(
                        'x',
                        MerchantVerificationProfile.MaxReviewNoteLength + 1)));

        Assert.Equal(
            MerchantVerificationStatus.UnderReview,
            profile.Status);

        Assert.Equal(
            reviewStartedAtUtc,
            profile.ReviewStartedAtUtc);

        Assert.Equal(
            reviewerUserId.Value,
            profile.ReviewedByUserId);

        Assert.Null(
            profile.ReviewedAtUtc);

        Assert.Null(
            profile.VerifiedAtUtc);

        Assert.Null(
            profile.ReviewNote);
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
}