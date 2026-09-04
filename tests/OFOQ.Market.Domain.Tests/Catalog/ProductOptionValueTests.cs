using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Catalog;

public sealed class ProductOptionValueTests
{
    [Fact]
    public void Create_NormalizesValueAndKey()
    {
        var value =
            ProductOptionValue.Create(
                TenantId.New(),
                ProductId.New(),
                ProductOptionId.New(),
                "  Black  ",
                0,
                DateTimeOffset.UtcNow);

        Assert.Equal(
            "Black",
            value.Value);

        Assert.Equal(
            "black",
            value.NormalizedValue);
    }

    [Fact]
    public void Create_WithNegativeSortOrder_Throws()
    {
        Assert.Throws<
            ArgumentOutOfRangeException>(
                () =>
                    ProductOptionValue.Create(
                        TenantId.New(),
                        ProductId.New(),
                        ProductOptionId.New(),
                        "Black",
                        -1,
                        DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Rename_UpdatesNormalizedValue()
    {
        var now =
            DateTimeOffset.UtcNow;

        var value =
            ProductOptionValue.Create(
                TenantId.New(),
                ProductId.New(),
                ProductOptionId.New(),
                "Black",
                0,
                now);

        value.Rename(
            "WHITE",
            now.AddMinutes(1));

        Assert.Equal(
            "WHITE",
            value.Value);

        Assert.Equal(
            "white",
            value.NormalizedValue);
    }

    [Fact]
    public void Delete_PreventsFurtherModification()
    {
        var now =
            DateTimeOffset.UtcNow;

        var value =
            ProductOptionValue.Create(
                TenantId.New(),
                ProductId.New(),
                ProductOptionId.New(),
                "Black",
                0,
                now);

        value.Delete(
            now.AddMinutes(1));

        Assert.True(
            value.IsDeleted);

        Assert.Throws<
            InvalidOperationException>(
                () =>
                    value.Rename(
                        "White",
                        now.AddMinutes(2)));
    }
}