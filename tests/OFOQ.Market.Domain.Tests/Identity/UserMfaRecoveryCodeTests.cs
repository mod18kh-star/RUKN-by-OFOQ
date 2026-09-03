using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Domain.Tests.Identity;

public sealed class UserMfaRecoveryCodeTests
{
    [Fact]
    public void Create_CreatesUnusedRecoveryCode()
    {
        var mfaId =
            UserMfaId.New();

        var now =
            DateTimeOffset.UtcNow;

        var recoveryCode =
            UserMfaRecoveryCode.Create(
                mfaId,
                "HASHED-RECOVERY-CODE",
                now);

        Assert.NotEqual(
            Guid.Empty,
            recoveryCode.Id.Value);

        Assert.Equal(
            mfaId,
            recoveryCode.UserMfaId);

        Assert.Equal(
            "HASHED-RECOVERY-CODE",
            recoveryCode.CodeHash);

        Assert.False(
            recoveryCode.IsUsed);

        Assert.Null(
            recoveryCode.UsedAtUtc);
    }

    [Fact]
    public void Create_RejectsEmptyMfaId()
    {
        Assert.Throws<ArgumentException>(
            () =>
                UserMfaRecoveryCode.Create(
                    default,
                    "HASHED-RECOVERY-CODE",
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_RejectsMissingHash()
    {
        Assert.Throws<ArgumentException>(
            () =>
                UserMfaRecoveryCode.Create(
                    UserMfaId.New(),
                    " ",
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkUsed_MarksCodeAsUsed()
    {
        var recoveryCode =
            CreateRecoveryCode();

        var now =
            DateTimeOffset.UtcNow;

        recoveryCode.MarkUsed(
            now);

        Assert.True(
            recoveryCode.IsUsed);

        Assert.Equal(
            now,
            recoveryCode.UsedAtUtc);

        Assert.Equal(
            now,
            recoveryCode.UpdatedAtUtc);
    }

    [Fact]
    public void MarkUsed_RejectsSecondUse()
    {
        var recoveryCode =
            CreateRecoveryCode();

        var now =
            DateTimeOffset.UtcNow;

        recoveryCode.MarkUsed(
            now);

        Assert.Throws<InvalidOperationException>(
            () =>
                recoveryCode.MarkUsed(
                    now.AddSeconds(1)));
    }

    private static UserMfaRecoveryCode CreateRecoveryCode()
    {
        return UserMfaRecoveryCode.Create(
            UserMfaId.New(),
            "HASHED-RECOVERY-CODE",
            DateTimeOffset.UtcNow);
    }
}