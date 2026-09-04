using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Catalog;

public sealed class ProductOptionTests
{
    [Fact]
    public void Create_NormalizesNameAndKey()
    {
        var option =
            ProductOption.Create(
                TenantId.New(),
                ProductId.New(),
                "  Color  ",
                0,
                DateTimeOffset.UtcNow);

        Assert.Equal(
            "Color",
            option.Name);

        Assert.Equal(
            "color",
            option.NormalizedName);

        Assert.Equal(
            0,
            option.SortOrder);
    }

    [Fact]
    public void Create_WithNegativeSortOrder_Throws()
    {
        Assert.Throws<
            ArgumentOutOfRangeException>(
                () =>
                    ProductOption.Create(
                        TenantId.New(),
                        ProductId.New(),
                        "Color",
                        -1,
                        DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Rename_UpdatesNormalizedName()
    {
        var now =
            DateTimeOffset.UtcNow;

        var option =
            ProductOption.Create(
                TenantId.New(),
                ProductId.New(),
                "Color",
                0,
                now);

        option.Rename(
            "SIZE",
            now.AddMinutes(1));

        Assert.Equal(
            "SIZE",
            option.Name);

        Assert.Equal(
            "size",
            option.NormalizedName);
    }

    [Fact]
    public void Delete_PreventsFurtherModification()
    {
        var now =
            DateTimeOffset.UtcNow;

        var option =
            ProductOption.Create(
                TenantId.New(),
                ProductId.New(),
                "Color",
                0,
                now);

        option.Delete(
            now.AddMinutes(1));

        Assert.True(
            option.IsDeleted);

        Assert.Throws<
            InvalidOperationException>(
                () =>
                    option.Rename(
                        "Size",
                        now.AddMinutes(2)));
    }
}