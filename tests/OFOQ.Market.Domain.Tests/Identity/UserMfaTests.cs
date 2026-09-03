using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Domain.Tests.Identity;

public sealed class UserMfaTests
{
    [Fact]
    public void BeginEnrollment_CreatesPendingMfa()
    {
        var userId =
            UserId.New();

        var now =
            DateTimeOffset.UtcNow;

        var mfa =
            UserMfa.BeginEnrollment(
                userId,
                "PROTECTED-SECRET",
                now);

        Assert.NotEqual(
            Guid.Empty,
            mfa.Id.Value);

        Assert.Equal(
            userId,
            mfa.UserId);

        Assert.Equal(
            "PROTECTED-SECRET",
            mfa.ProtectedSecret);

        Assert.Equal(
            UserMfaStatus.PendingEnrollment,
            mfa.Status);

        Assert.Null(
            mfa.EnabledAtUtc);

        Assert.Null(
            mfa.LastAcceptedTimeStep);

        Assert.Equal(
            now,
            mfa.CreatedAtUtc);
    }

    [Fact]
    public void BeginEnrollment_RejectsEmptyUserId()
    {
        Assert.Throws<ArgumentException>(
            () =>
                UserMfa.BeginEnrollment(
                    default,
                    "PROTECTED-SECRET",
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void BeginEnrollment_RejectsMissingSecret()
    {
        Assert.Throws<ArgumentException>(
            () =>
                UserMfa.BeginEnrollment(
                    UserId.New(),
                    " ",
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Enable_EnablesMfa()
    {
        var mfa =
            CreatePendingMfa();

        var now =
            DateTimeOffset.UtcNow;

        mfa.Enable(
            now);

        Assert.Equal(
            UserMfaStatus.Enabled,
            mfa.Status);

        Assert.Equal(
            now,
            mfa.EnabledAtUtc);

        Assert.Equal(
            now,
            mfa.UpdatedAtUtc);
    }

    [Fact]
    public void Enable_RejectsSecondEnable()
    {
        var mfa =
            CreatePendingMfa();

        var now =
            DateTimeOffset.UtcNow;

        mfa.Enable(
            now);

        Assert.Throws<InvalidOperationException>(
            () =>
                mfa.Enable(
                    now.AddSeconds(1)));
    }

    [Fact]
    public void AcceptTimeStep_StoresAcceptedStep()
    {
        var mfa =
            CreateEnabledMfa();

        var now =
            DateTimeOffset.UtcNow;

        mfa.AcceptTimeStep(
            123456,
            now);

        Assert.Equal(
            123456,
            mfa.LastAcceptedTimeStep);

        Assert.Equal(
            now,
            mfa.UpdatedAtUtc);
    }

    [Fact]
    public void AcceptTimeStep_RejectsReplay()
    {
        var mfa =
            CreateEnabledMfa();

        var now =
            DateTimeOffset.UtcNow;

        mfa.AcceptTimeStep(
            123456,
            now);

        Assert.Throws<InvalidOperationException>(
            () =>
                mfa.AcceptTimeStep(
                    123456,
                    now.AddSeconds(1)));
    }

    [Fact]
    public void AcceptTimeStep_RejectsOlderStep()
    {
        var mfa =
            CreateEnabledMfa();

        var now =
            DateTimeOffset.UtcNow;

        mfa.AcceptTimeStep(
            123456,
            now);

        Assert.Throws<InvalidOperationException>(
            () =>
                mfa.AcceptTimeStep(
                    123455,
                    now.AddSeconds(1)));
    }

    [Fact]
    public void AcceptTimeStep_RejectsWhenMfaIsNotEnabled()
    {
        var mfa =
            CreatePendingMfa();

        Assert.Throws<InvalidOperationException>(
            () =>
                mfa.AcceptTimeStep(
                    123456,
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AcceptTimeStep_RejectsNegativeStep()
    {
        var mfa =
            CreateEnabledMfa();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                mfa.AcceptTimeStep(
                    -1,
                    DateTimeOffset.UtcNow));
    }

    private static UserMfa CreatePendingMfa()
    {
        return UserMfa.BeginEnrollment(
            UserId.New(),
            "PROTECTED-SECRET",
            DateTimeOffset.UtcNow);
    }

    private static UserMfa CreateEnabledMfa()
    {
        var mfa =
            CreatePendingMfa();

        mfa.Enable(
            DateTimeOffset.UtcNow);

        return mfa;
    }
}