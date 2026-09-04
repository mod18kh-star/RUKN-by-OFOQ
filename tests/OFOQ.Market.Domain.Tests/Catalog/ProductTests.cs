using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Catalog;

public sealed class ProductTests
{
    [Fact]
    public void Create_CreatesDraftHiddenProduct()
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenantId =
            TenantId.New();

        var product =
            Product.Create(
                tenantId,
                "  iPhone 17 Pro  ",
                "IPHONE-17-PRO",
                Money.Create(
                    1200m,
                    "USD"),
                now);

        Assert.Equal(
            tenantId,
            product.TenantId);

        Assert.Equal(
            "iPhone 17 Pro",
            product.Name);

        Assert.Equal(
            "iphone-17-pro",
            product.Slug);

        Assert.Equal(
            1200m,
            product.Price.Amount);

        Assert.Equal(
            "USD",
            product.Price.Currency.Value);

        Assert.Equal(
            ProductStatus.Draft,
            product.Status);

        Assert.False(
            product.IsVisible);

        Assert.False(
            product.IsDeleted);
    }

    [Fact]
    public void Create_NormalizesCurrencyCode()
    {
        var product =
            Product.Create(
                TenantId.New(),
                "Phone",
                "phone",
                Money.Create(
                    100m,
                    "sar"),
                DateTimeOffset.UtcNow);

        Assert.Equal(
            "SAR",
            product.Price.Currency.Value);
    }

    [Fact]
    public void Create_WithCompareAtPrice_SavesValidDiscountPricing()
    {
        var product =
            Product.Create(
                TenantId.New(),
                "Phone",
                "phone",
                Money.Create(
                    900m,
                    "USD"),
                DateTimeOffset.UtcNow,
                compareAtPrice:
                    Money.Create(
                        1000m,
                        "USD"));

        Assert.NotNull(
            product.CompareAtPrice);

        Assert.Equal(
            1000m,
            product.CompareAtPrice!.Value.Amount);
    }

    [Fact]
    public void Create_WithCompareAtPriceInDifferentCurrency_Throws()
    {
        Assert.Throws<
            ArgumentException>(
                () =>
                    Product.Create(
                        TenantId.New(),
                        "Phone",
                        "phone",
                        Money.Create(
                            900m,
                            "USD"),
                        DateTimeOffset.UtcNow,
                        compareAtPrice:
                            Money.Create(
                                1000m,
                                "SAR")));
    }

    [Fact]
    public void Create_WithCompareAtPriceLowerThanPrice_Throws()
    {
        Assert.Throws<
            ArgumentException>(
                () =>
                    Product.Create(
                        TenantId.New(),
                        "Phone",
                        "phone",
                        Money.Create(
                            1000m,
                            "USD"),
                        DateTimeOffset.UtcNow,
                        compareAtPrice:
                            Money.Create(
                                900m,
                                "USD")));
    }

    [Fact]
    public void Publish_MarksProductPublishedAndVisible()
    {
        var now =
            DateTimeOffset.UtcNow;

        var product =
            Product.Create(
                TenantId.New(),
                "Phone",
                "phone",
                Money.Create(
                    100m,
                    "USD"),
                now);

        product.Publish(
            now.AddMinutes(1));

        Assert.Equal(
            ProductStatus.Published,
            product.Status);

        Assert.True(
            product.IsVisible);
    }

    [Fact]
    public void Show_DraftProduct_Throws()
    {
        var product =
            Product.Create(
                TenantId.New(),
                "Phone",
                "phone",
                Money.Create(
                    100m,
                    "USD"),
                DateTimeOffset.UtcNow);

        Assert.Throws<
            InvalidOperationException>(
                () =>
                    product.Show(
                        DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Archive_HidesProduct()
    {
        var now =
            DateTimeOffset.UtcNow;

        var product =
            Product.Create(
                TenantId.New(),
                "Phone",
                "phone",
                Money.Create(
                    100m,
                    "USD"),
                now);

        product.Publish(
            now.AddMinutes(1));

        product.Archive(
            now.AddMinutes(2));

        Assert.Equal(
            ProductStatus.Archived,
            product.Status);

        Assert.False(
            product.IsVisible);
    }

    [Fact]
    public void Delete_MarksProductDeletedAndHidden()
    {
        var now =
            DateTimeOffset.UtcNow;

        var product =
            Product.Create(
                TenantId.New(),
                "Phone",
                "phone",
                Money.Create(
                    100m,
                    "USD"),
                now);

        product.Publish(
            now.AddMinutes(1));

        product.Delete(
            now.AddMinutes(2));

        Assert.True(
            product.IsDeleted);

        Assert.False(
            product.IsVisible);

        Assert.Equal(
            now.AddMinutes(2),
            product.DeletedAtUtc);
    }

    [Fact]
    public void Publish_DeletedProduct_Throws()
    {
        var now =
            DateTimeOffset.UtcNow;

        var product =
            Product.Create(
                TenantId.New(),
                "Phone",
                "phone",
                Money.Create(
                    100m,
                    "USD"),
                now);

        product.Delete(
            now.AddMinutes(1));

        Assert.Throws<
            InvalidOperationException>(
                () =>
                    product.Publish(
                        now.AddMinutes(2)));
    }

    [Fact]
    public void Money_NegativeAmount_Throws()
    {
        Assert.Throws<
            ArgumentOutOfRangeException>(
                () =>
                    Money.Create(
                        -1m,
                        "USD"));
    }

    [Fact]
    public void CurrencyCode_WithInvalidValue_Throws()
    {
        Assert.Throws<
            ArgumentException>(
                () =>
                    CurrencyCode.Create(
                        "US"));

        Assert.Throws<
            ArgumentException>(
                () =>
                    CurrencyCode.Create(
                        "12A"));
    }
}