using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Domain.Tests.Identity;

public sealed class UserTests
{
    [Fact]
    public void Create_NormalizesEmail()
    {
        var user =
            User.Create(
                "  TEST@Example.COM  ",
                "hashed-password",
                DateTimeOffset.UtcNow);

        Assert.Equal(
            "test@example.com",
            user.Email.Value);
    }

    [Fact]
    public void Create_RaisesUserRegisteredDomainEvent()
    {
        var user =
            User.Create(
                "test@example.com",
                "hashed-password",
                DateTimeOffset.UtcNow);

        Assert.Contains(
            user.DomainEvents,
            domainEvent =>
                domainEvent is UserRegisteredDomainEvent);
    }

    [Fact]
    public void Create_RejectsInvalidEmail()
    {
        Assert.Throws<ArgumentException>(
            () =>
                User.Create(
                    "invalid-email",
                    "hashed-password",
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkEmailVerified_SetsVerificationTime()
    {
        var user =
            User.Create(
                "test@example.com",
                "hashed-password",
                DateTimeOffset.UtcNow);

        var verifiedAt =
            DateTimeOffset.UtcNow.AddMinutes(1);

        user.MarkEmailVerified(verifiedAt);

        Assert.Equal(
            verifiedAt,
            user.EmailVerifiedAtUtc);
    }

    [Fact]
    public void Suspend_ChangesStatus()
    {
        var user =
            User.Create(
                "test@example.com",
                "hashed-password",
                DateTimeOffset.UtcNow);

        user.Suspend(
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal(
            UserStatus.Suspended,
            user.Status);
    }

    [Fact]
    public void ChangePasswordHash_ReplacesHash()
    {
        var user =
            User.Create(
                "test@example.com",
                "old-hash",
                DateTimeOffset.UtcNow);

        user.ChangePasswordHash(
            "new-hash",
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal(
            "new-hash",
            user.PasswordHash);
    }
}