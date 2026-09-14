using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Catalog;

public sealed class ProductImageTests
{
    [Fact]
    public void Create_NormalizesImageData()
    {
        var image =
            ProductImage.Create(
                TenantId.New(),
                ProductId.New(),
                " https://example.com/product.jpg ",
                " Product front ",
                0,
                true,
                DateTimeOffset.UtcNow);

        Assert.Equal(
            "https://example.com/product.jpg",
            image.Url);

        Assert.Equal(
            "Product front",
            image.AltText);

        Assert.True(
            image.IsPrimary);

        Assert.Equal(
            0,
            image.SortOrder);
    }

    [Fact]
    public void Create_InvalidUrl_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                ProductImage.Create(
                    TenantId.New(),
                    ProductId.New(),
                    "not-a-url",
                    null,
                    0,
                    true,
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_NegativeSortOrder_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                ProductImage.Create(
                    TenantId.New(),
                    ProductId.New(),
                    "https://example.com/product.jpg",
                    null,
                    -1,
                    false,
                    DateTimeOffset.UtcNow));
    }
}