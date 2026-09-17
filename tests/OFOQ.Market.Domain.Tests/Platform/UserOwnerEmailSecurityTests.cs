using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Domain.Tests.Platform;

public sealed class UserOwnerEmailSecurityTests
{
    [Fact]
    public void ChangeEmail_NormalizesAndClearsVerification()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create(
            "owner@example.com",
            "password-hash",
            now);

        user.MarkEmailVerified(now.AddMinutes(1));
        user.ChangeEmail(
            "  NEW.OWNER@Example.COM ",
            now.AddMinutes(2));

        Assert.Equal("new.owner@example.com", user.Email.Value);
        Assert.Null(user.EmailVerifiedAtUtc);
    }
}
