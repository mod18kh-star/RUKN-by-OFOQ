using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Domain.Tests.Identity;

public sealed class UserSessionTests
{
    [Fact]
    public void Create_StartsUsable_AndPreservesAuthenticationLevel()
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                16,
                1,
                0,
                0,
                TimeSpan.Zero);

        var session =
            UserSession.Create(
                UserSessionId.New(),
                UserId.New(),
                new string('A', 64),
                UserSessionAuthenticationLevel.PasswordOnly,
                now.AddDays(30),
                now,
                "127.0.0.1",
                "test-agent");

        Assert.True(
            session.IsUsable(now));

        Assert.Equal(
            UserSessionAuthenticationLevel.PasswordOnly,
            session.AuthenticationLevel);

        Assert.Equal(
            "127.0.0.1",
            session.LastIpAddress);
    }

    [Fact]
    public void RotateRefreshToken_UpdatesLastSeenAndHash()
    {
        var now = DateTimeOffset.UtcNow;

        var session =
            UserSession.Create(
                UserId.New(),
                new string('A', 64),
                UserSessionAuthenticationLevel.PasswordOnly,
                now.AddDays(30),
                now);

        var rotatedAt =
            now.AddMinutes(10);

        session.RotateRefreshToken(
            new string('B', 64),
            rotatedAt,
            "10.0.0.2",
            "new-agent");

        Assert.Equal(
            new string('B', 64),
            session.RefreshTokenHash);

        Assert.Equal(
            rotatedAt,
            session.LastSeenAtUtc);

        Assert.Equal(
            "10.0.0.2",
            session.LastIpAddress);
    }

    [Fact]
    public void UpgradeToMultiFactor_RotatesTokenAndRaisesLevel()
    {
        var now = DateTimeOffset.UtcNow;

        var session =
            UserSession.Create(
                UserId.New(),
                new string('A', 64),
                UserSessionAuthenticationLevel.PasswordOnly,
                now.AddDays(30),
                now);

        session.UpgradeToMultiFactor(
            new string('C', 64),
            now.AddMinutes(1));

        Assert.Equal(
            UserSessionAuthenticationLevel.MultiFactor,
            session.AuthenticationLevel);

        Assert.Equal(
            new string('C', 64),
            session.RefreshTokenHash);
    }

    [Fact]
    public void Revoke_MakesSessionUnusable()
    {
        var now = DateTimeOffset.UtcNow;

        var session =
            UserSession.Create(
                UserId.New(),
                new string('A', 64),
                UserSessionAuthenticationLevel.PasswordOnly,
                now.AddDays(30),
                now);

        session.Revoke(
            "test",
            now.AddMinutes(1));

        Assert.True(session.IsRevoked);
        Assert.False(
            session.IsUsable(
                now.AddMinutes(2)));
    }
}
