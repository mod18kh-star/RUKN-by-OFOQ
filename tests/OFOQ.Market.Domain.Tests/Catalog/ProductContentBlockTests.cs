using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Catalog;

public sealed class ProductContentBlockTests
{
    [Fact]
    public void Create_SizeGuideWithImage_NormalizesValues()
    {
        var block =
            ProductContentBlock.Create(
                TenantId.New(),
                ProductId.New(),
                ProductContentBlockType.SizeGuide,
                " جدول المقاسات ",
                null,
                " https://example.com/size-guide.jpg ",
                0,
                true,
                DateTimeOffset.UtcNow);

        Assert.Equal(
            "جدول المقاسات",
            block.Title);

        Assert.Equal(
            "https://example.com/size-guide.jpg",
            block.MediaUrl);

        Assert.True(
            block.IsVisible);
    }

    [Fact]
    public void Create_SizeGuideWithoutMedia_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                ProductContentBlock.Create(
                    TenantId.New(),
                    ProductId.New(),
                    ProductContentBlockType.SizeGuide,
                    "Sizes",
                    "Text only",
                    null,
                    0,
                    true,
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_WithoutBodyOrMedia_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                ProductContentBlock.Create(
                    TenantId.New(),
                    ProductId.New(),
                    ProductContentBlockType.RichText,
                    "Empty",
                    null,
                    null,
                    0,
                    true,
                    DateTimeOffset.UtcNow));
    }
}
