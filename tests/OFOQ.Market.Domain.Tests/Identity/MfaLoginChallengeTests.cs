using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Domain.Tests.Identity;

public sealed class MfaLoginChallengeTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            4,
            0,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void Create_CreatesUsableChallenge()
    {
        var userId =
            UserId.New();

        var challenge =
            MfaLoginChallenge.Create(
                userId,
                "TOKEN-HASH",
                Now.AddMinutes(5),
                Now);

        Assert.NotEqual(
            Guid.Empty,
            challenge.Id.Value);

        Assert.Equal(
            userId,
            challenge.UserId);

        Assert.Equal(
            "TOKEN-HASH",
            challenge.TokenHash);

        Assert.Equal(
            Now.AddMinutes(5),
            challenge.ExpiresAtUtc);

        Assert.Equal(
            0,
            challenge.FailedAttemptCount);

        Assert.False(
            challenge.IsConsumed);

        Assert.False(
            challenge.IsRevoked);

        Assert.False(
            challenge.IsExhausted);

        Assert.True(
            challenge.IsUsable(
                Now));
    }

    [Fact]
    public void Create_RejectsInvalidExpiration()
    {
        Assert.Throws<ArgumentException>(
            () =>
                MfaLoginChallenge.Create(
                    UserId.New(),
                    "TOKEN-HASH",
                    Now,
                    Now));
    }

    [Fact]
    public void RegisterFailedAttempt_IncrementsCounter()
    {
        var challenge =
            CreateChallenge();

        challenge.RegisterFailedAttempt(
            Now.AddSeconds(10));

        Assert.Equal(
            1,
            challenge.FailedAttemptCount);

        Assert.True(
            challenge.IsUsable(
                Now.AddSeconds(11)));
    }

    [Fact]
    public void FifthFailedAttempt_ExhaustsChallenge()
    {
        var challenge =
            CreateChallenge();

        for (var index = 0;
             index < MfaLoginChallenge.MaximumFailedAttempts;
             index++)
        {
            challenge.RegisterFailedAttempt(
                Now.AddSeconds(
                    index + 1));
        }

        Assert.Equal(
            MfaLoginChallenge.MaximumFailedAttempts,
            challenge.FailedAttemptCount);

        Assert.True(
            challenge.IsExhausted);

        Assert.False(
            challenge.IsUsable(
                Now.AddSeconds(10)));
    }

    [Fact]
    public void ExhaustedChallenge_RejectsAdditionalAttempt()
    {
        var challenge =
            CreateChallenge();

        for (var index = 0;
             index < MfaLoginChallenge.MaximumFailedAttempts;
             index++)
        {
            challenge.RegisterFailedAttempt(
                Now.AddSeconds(
                    index + 1));
        }

        Assert.Throws<InvalidOperationException>(
            () =>
                challenge.RegisterFailedAttempt(
                    Now.AddSeconds(10)));
    }

    [Fact]
    public void Consume_MakesChallengeSingleUse()
    {
        var challenge =
            CreateChallenge();

        var consumedAtUtc =
            Now.AddMinutes(1);

        challenge.Consume(
            consumedAtUtc);

        Assert.True(
            challenge.IsConsumed);

        Assert.Equal(
            consumedAtUtc,
            challenge.ConsumedAtUtc);

        Assert.False(
            challenge.IsUsable(
                consumedAtUtc.AddSeconds(1)));

        Assert.Throws<InvalidOperationException>(
            () =>
                challenge.Consume(
                    consumedAtUtc.AddSeconds(2)));
    }

    [Fact]
    public void ExpiredChallenge_CannotBeConsumed()
    {
        var challenge =
            CreateChallenge();

        Assert.False(
            challenge.IsUsable(
                Now.AddMinutes(5)));

        Assert.Throws<InvalidOperationException>(
            () =>
                challenge.Consume(
                    Now.AddMinutes(5)));
    }

    [Fact]
    public void Revoke_DisablesChallenge()
    {
        var challenge =
            CreateChallenge();

        challenge.Revoke(
            Now.AddMinutes(1));

        Assert.True(
            challenge.IsRevoked);

        Assert.False(
            challenge.IsUsable(
                Now.AddMinutes(1)
                    .AddSeconds(1)));

        Assert.Throws<InvalidOperationException>(
            () =>
                challenge.Consume(
                    Now.AddMinutes(2)));
    }

    private static MfaLoginChallenge CreateChallenge()
    {
        return MfaLoginChallenge.Create(
            UserId.New(),
            "TOKEN-HASH",
            Now.AddMinutes(5),
            Now);
    }
}