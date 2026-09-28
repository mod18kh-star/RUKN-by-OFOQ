using OFOQ.Market.Domain.Identity;
using Xunit;

namespace OFOQ.Market.Domain.Tests.Identity;

public sealed class MfaSessionPolicyTests
{
    [Fact]
    public void MultiFactorSession_ExpiresExactlyAtSixHourBoundary()
    {
        var createdAt =
            new DateTimeOffset(
                2026,
                1,
                1,
                0,
                0,
                0,
                TimeSpan.Zero);

        var session =
            UserSession.Create(
                UserId.From(Guid.NewGuid()),
                "refresh-hash-1",
                UserSessionAuthenticationLevel.MultiFactor,
                createdAt.AddDays(1),
                createdAt);

        Assert.True(
            session.HasFreshMultiFactor(
                createdAt
                    .AddHours(6)
                    .AddTicks(-1)));

        Assert.False(
            session.HasFreshMultiFactor(
                createdAt.AddHours(6)));
    }

    [Fact]
    public void UpgradeToMultiFactor_ResetsVerificationClock()
    {
        var createdAt =
            new DateTimeOffset(
                2026,
                1,
                1,
                0,
                0,
                0,
                TimeSpan.Zero);

        var verificationAt =
            createdAt.AddHours(2);

        var session =
            UserSession.Create(
                UserId.From(Guid.NewGuid()),
                "refresh-hash-1",
                UserSessionAuthenticationLevel.PasswordOnly,
                createdAt.AddDays(1),
                createdAt);

        Assert.Null(
            session.LastMfaVerifiedAtUtc);

        session.UpgradeToMultiFactor(
            "refresh-hash-2",
            verificationAt);

        Assert.Equal(
            verificationAt,
            session.LastMfaVerifiedAtUtc);

        Assert.True(
            session.HasFreshMultiFactor(
                verificationAt.AddHours(5)));

        Assert.False(
            session.HasFreshMultiFactor(
                verificationAt.AddHours(6)));
    }

    [Fact]
    public void MfaReopenPolicy_CanBeUpdated()
    {
        var now =
            new DateTimeOffset(
                2026,
                1,
                1,
                0,
                0,
                0,
                TimeSpan.Zero);

        var userId =
            UserId.From(
                Guid.NewGuid());

        var mfa =
            UserMfa.BeginEnrollment(
                userId,
                "protected-secret",
                now,
                userId.Value);

        mfa.UpdateReopenPolicy(
            MfaReopenPolicy.Minutes30,
            now.AddMinutes(1),
            userId.Value);

        Assert.Equal(
            MfaReopenPolicy.Minutes30,
            mfa.ReopenPolicy);
    }
}