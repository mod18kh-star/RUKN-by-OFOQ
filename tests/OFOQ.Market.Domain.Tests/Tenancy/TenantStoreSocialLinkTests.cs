using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Tenancy;

public sealed class TenantStoreSocialLinkTests
{
    [Fact]
    public void Create_NormalizesPlatformAndUrl()
    {
        var link =
            TenantStoreSocialLink.Create(
                TenantId.New(),
                " Instagram ",
                " الحساب الرئيسي ",
                " https://instagram.com/rukn ",
                0,
                true,
                DateTimeOffset.UtcNow);

        Assert.Equal(
            "instagram",
            link.PlatformCode);

        Assert.Equal(
            "الحساب الرئيسي",
            link.Label);

        Assert.Equal(
            "https://instagram.com/rukn",
            link.Url);
    }

    [Fact]
    public void Create_WithInvalidUrl_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                TenantStoreSocialLink.Create(
                    TenantId.New(),
                    "instagram",
                    null,
                    "instagram.com/rukn",
                    0,
                    true,
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WithNegativeSortOrder_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                TenantStoreSocialLink.Create(
                    TenantId.New(),
                    "instagram",
                    null,
                    "https://instagram.com/rukn",
                    -1,
                    true,
                    DateTimeOffset.UtcNow));
    }
}
