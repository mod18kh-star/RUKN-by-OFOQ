using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Tenancy;

public sealed class TenantStoreProfileTests
{
    [Fact]
    public void Create_NormalizesOptionalValues()
    {
        var tenantId =
            TenantId.New();

        var now =
            DateTimeOffset.UtcNow;

        var profile =
            TenantStoreProfile.Create(
                tenantId,
                " https://example.com/store ",
                " +966500000000 ",
                " 920000000 ",
                " 1010123456 ",
                false,
                now);

        Assert.Equal(
            "https://example.com/store",
            profile.WebsiteUrl);

        Assert.Equal(
            "+966500000000",
            profile.WhatsAppNumber);

        Assert.Equal(
            "920000000",
            profile.CustomerServicePhone);

        Assert.Equal(
            "1010123456",
            profile.CommercialRegistrationNumber);
    }

    [Fact]
    public void Create_WithRegistrationMarkedNotApplicable_RejectsRegistrationNumber()
    {
        Assert.Throws<ArgumentException>(
            () =>
                TenantStoreProfile.Create(
                    TenantId.New(),
                    null,
                    null,
                    null,
                    "123",
                    true,
                    DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("ftp://example.com")]
    public void Create_WithInvalidWebsiteUrl_Throws(
        string websiteUrl)
    {
        Assert.Throws<ArgumentException>(
            () =>
                TenantStoreProfile.Create(
                    TenantId.New(),
                    websiteUrl,
                    null,
                    null,
                    null,
                    false,
                    DateTimeOffset.UtcNow));
    }
}
