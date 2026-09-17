using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Domain.Tests.Identity;

public sealed class EmailVerificationChallengeTests
{
    [Fact]
    public void Create_NormalizesEmail_AndStartsUsable()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                16,
                0,
                0,
                0,
                TimeSpan.Zero);

        var challenge =
            EmailVerificationChallenge.Create(
                UserId.New(),
                "  USER@Example.COM ",
                new string(
                    'A',
                    64),
                now.AddMinutes(
                    30),
                now);

        Assert.Equal(
            "user@example.com",
            challenge.EmailSnapshot);

        Assert.True(
            challenge.IsUsable(
                now));
    }

    [Fact]
    public void Consume_MakesChallengeUnusable()
    {
        var now =
            DateTimeOffset.UtcNow;

        var challenge =
            EmailVerificationChallenge.Create(
                UserId.New(),
                "user@example.com",
                new string(
                    'A',
                    64),
                now.AddMinutes(
                    30),
                now);

        challenge.Consume(
            now.AddMinutes(
                1));

        Assert.True(
            challenge.IsConsumed);

        Assert.False(
            challenge.IsUsable(
                now.AddMinutes(
                    2)));
    }

    [Fact]
    public void Revoke_MakesChallengeUnusable()
    {
        var now =
            DateTimeOffset.UtcNow;

        var challenge =
            EmailVerificationChallenge.Create(
                UserId.New(),
                "user@example.com",
                new string(
                    'A',
                    64),
                now.AddMinutes(
                    30),
                now);

        challenge.Revoke(
            now.AddMinutes(
                1));

        Assert.True(
            challenge.IsRevoked);

        Assert.False(
            challenge.IsUsable(
                now.AddMinutes(
                    2)));
    }
}
