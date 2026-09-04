using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Catalog;

public sealed class ProductVariantTests
{
    [Fact]
    public void Create_CreatesEnabledVariant()
    {
        var tenantId =
            TenantId.New();

        var productId =
            ProductId.New();

        var variant =
            ProductVariant.Create(
                tenantId,
                productId,
                "Black / M",
                ProductSku.Create(
                    "shirt-black-m"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 10),
                DateTimeOffset.UtcNow);

        Assert.Equal(
            tenantId,
            variant.TenantId);

        Assert.Equal(
            productId,
            variant.ProductId);

        Assert.Equal(
            "Black / M",
            variant.Name);

        Assert.Equal(
            "SHIRT-BLACK-M",
            variant.Sku.Value);

        Assert.True(
            variant.IsEnabled);

        Assert.Equal(
            10,
            variant.Inventory.Quantity);
    }

    [Fact]
    public void Create_WithPriceOverrideInDifferentCurrency_Throws()
    {
        Assert.Throws<
            ArgumentException>(
                () =>
                    ProductVariant.Create(
                        TenantId.New(),
                        ProductId.New(),
                        "Black / M",
                        ProductSku.Create(
                            "shirt-black-m"),
                        CurrencyCode.Create(
                            "USD"),
                        Inventory.Create(
                            trackInventory: true),
                        DateTimeOffset.UtcNow,
                        priceOverride:
                            Money.Create(
                                100m,
                                "SAR")));
    }

    [Fact]
    public void IncreaseStock_IncreasesInventory()
    {
        var now =
            DateTimeOffset.UtcNow;

        var variant =
            CreateVariant(
                quantity: 5);

        variant.IncreaseStock(
            3,
            now);

        Assert.Equal(
            8,
            variant.Inventory.Quantity);
    }

    [Fact]
    public void DecreaseStock_DecreasesInventory()
    {
        var now =
            DateTimeOffset.UtcNow;

        var variant =
            CreateVariant(
                quantity: 5);

        variant.DecreaseStock(
            2,
            now);

        Assert.Equal(
            3,
            variant.Inventory.Quantity);
    }

    [Fact]
    public void DecreaseStock_WhenInsufficient_Throws()
    {
        var variant =
            CreateVariant(
                quantity: 2);

        Assert.Throws<
            InvalidOperationException>(
                () =>
                    variant.DecreaseStock(
                        3,
                        DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Disable_PreventsVariantModification()
    {
        var now =
            DateTimeOffset.UtcNow;

        var variant =
            CreateVariant(
                quantity: 5);

        variant.Disable(
            now);

        Assert.False(
            variant.IsEnabled);

        Assert.Throws<
            InvalidOperationException>(
                () =>
                    variant.IncreaseStock(
                        1,
                        now.AddMinutes(1)));
    }

    [Fact]
    public void Delete_DisablesAndSoftDeletesVariant()
    {
        var now =
            DateTimeOffset.UtcNow;

        var variant =
            CreateVariant(
                quantity: 5);

        variant.Delete(
            now);

        Assert.True(
            variant.IsDeleted);

        Assert.False(
            variant.IsEnabled);

        Assert.Equal(
            now,
            variant.DeletedAtUtc);
    }

    [Fact]
    public void ProductSku_NormalizesToUppercase()
    {
        var sku =
            ProductSku.Create(
                "  phone-black-256  ");

        Assert.Equal(
            "PHONE-BLACK-256",
            sku.Value);
    }

    private static ProductVariant CreateVariant(
        int quantity)
    {
        return ProductVariant.Create(
            TenantId.New(),
            ProductId.New(),
            "Black / M",
            ProductSku.Create(
                "shirt-black-m"),
            CurrencyCode.Create(
                "USD"),
            Inventory.Create(
                trackInventory: true,
                quantity: quantity),
            DateTimeOffset.UtcNow);
    }
}