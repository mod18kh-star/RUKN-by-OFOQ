using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Domain.Tests.Identity;

public sealed class UserTrustedDeviceTests
{
    [Fact]
    public void Create_ProducesUsableTrustedDeviceUntilExpiry()
    {
        var now =
            DateTimeOffset.UtcNow;

        var device =
            UserTrustedDevice.Create(
                UserTrustedDeviceId.New(),
                UserId.New(),
                "ABCDEF",
                "USERAGENT",
                now.AddDays(30),
                now,
                "127.0.0.1");

        Assert.True(
            device.IsUsable(
                now.AddDays(1)));

        Assert.False(
            device.IsRevoked);
    }

    [Fact]
    public void Revoke_DisablesTrustedDevice()
    {
        var now =
            DateTimeOffset.UtcNow;

        var device =
            UserTrustedDevice.Create(
                UserTrustedDeviceId.New(),
                UserId.New(),
                "ABCDEF",
                "USERAGENT",
                now.AddDays(30),
                now);

        device.Revoke(
            "test",
            now.AddMinutes(1));

        Assert.True(
            device.IsRevoked);

        Assert.False(
            device.IsUsable(
                now.AddMinutes(2)));
    }
}
