using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Tenancy;

public sealed class TenantStorefrontPresentationTests
{
    [Fact]
    public void Create_NormalizesBrandingAndDefaults()
    {
        var presentation =
            TenantStorefrontPresentation.Create(
                TenantId.New(),
                " https://example.com/logo.png ",
                " https://example.com/cover.jpg ",
                " شحن مجاني ",
                "#112233",
                "#a77a43",
                null,
                null,
                true,
                true,
                null,
                null,
                DateTimeOffset.UtcNow);

        Assert.Equal(
            "https://example.com/logo.png",
            presentation.LogoUrl);

        Assert.Equal(
            "https://example.com/cover.jpg",
            presentation.CoverImageUrl);

        Assert.Equal(
            "شحن مجاني",
            presentation.Announcement);

        Assert.Equal(
            "#112233",
            presentation.PrimaryColor);

        Assert.Equal(
            "#A77A43",
            presentation.AccentColor);

        Assert.Equal(
            TenantStorefrontPresentation.DefaultThemePresetCode,
            presentation.ThemePresetCode);

        Assert.Equal(
            TenantStorefrontPresentation.DefaultFontCode,
            presentation.FontCode);

        Assert.Equal(
            TenantStorefrontPresentation.DefaultCategorySectionTitle,
            presentation.CategorySectionTitle);

        Assert.Equal(
            TenantStorefrontPresentation.DefaultProductSectionTitle,
            presentation.ProductSectionTitle);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#123")]
    [InlineData("#GG0000")]
    public void Create_WithInvalidColor_Throws(
        string color)
    {
        Assert.Throws<ArgumentException>(
            () =>
                TenantStorefrontPresentation.Create(
                    TenantId.New(),
                    null,
                    null,
                    null,
                    color,
                    null,
                    null,
                    null,
                    true,
                    true,
                    null,
                    null,
                    DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("logo.png")]
    [InlineData("ftp://example.com/logo.png")]
    public void Create_WithInvalidLogoUrl_Throws(
        string logoUrl)
    {
        Assert.Throws<ArgumentException>(
            () =>
                TenantStorefrontPresentation.Create(
                    TenantId.New(),
                    logoUrl,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    true,
                    true,
                    null,
                    null,
                    DateTimeOffset.UtcNow));
    }
}
