using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Domain.Tests.Identity;

public sealed class UserExternalLoginTests
{
    [Fact]
    public void Create_NormalizesAndStoresVerifiedLinkIdentity()
    {
        var userId = UserId.New();
        var now = DateTimeOffset.Parse("2026-09-16T12:00:00Z");

        var login = UserExternalLogin.Create(
            userId,
            " google ",
            "subject-123",
            "Owner@Example.com",
            now);

        Assert.Equal(userId, login.UserId);
        Assert.Equal("google", login.Provider);
        Assert.Equal("subject-123", login.Subject);
        Assert.Equal("owner@example.com", login.EmailAtLink);
        Assert.Equal(now, login.CreatedAtUtc);
    }
}
