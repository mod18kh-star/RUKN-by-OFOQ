using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Commerce.Carts;

public sealed class CartTests
{
    [Fact]
    public void Create_StartsActiveAndEmpty()
    {
        var cart =
            CreateCart();

        Assert.Equal(
            CartStatus.Active,
            cart.Status);

        Assert.Empty(
            cart.Items);

        Assert.Equal(
            0,
            cart.TotalQuantity);

        Assert.Equal(
            0m,
            cart.TotalAmount);

        Assert.Null(
            cart.Currency);
    }

    [Fact]
    public void AddItem_SetsCurrencyQuantityAndTotal()
    {
        var cart =
            CreateCart();

        cart.AddItem(
            ProductId.New(),
            ProductVariantId.New(),
            Money.Create(
                25m,
                "USD"),
            2,
            DateTimeOffset.UtcNow);

        var item =
            Assert.Single(
                cart.Items);

        Assert.Equal(
            2,
            item.Quantity);

        Assert.Equal(
            50m,
            item.LineTotal);

        Assert.Equal(
            2,
            cart.TotalQuantity);

        Assert.Equal(
            50m,
            cart.TotalAmount);

        Assert.Equal(
            CurrencyCode.Create("USD"),
            cart.Currency);
    }

    [Fact]
    public void AddSameVariant_IncreasesExistingLine()
    {
        var cart =
            CreateCart();

        var productId =
            ProductId.New();

        var variantId =
            ProductVariantId.New();

        var now =
            DateTimeOffset.UtcNow;

        cart.AddItem(
            productId,
            variantId,
            Money.Create(
                10m,
                "USD"),
            2,
            now);

        cart.AddItem(
            productId,
            variantId,
            Money.Create(
                10m,
                "USD"),
            3,
            now.AddMinutes(1));

        var item =
            Assert.Single(
                cart.Items);

        Assert.Equal(
            5,
            item.Quantity);

        Assert.Equal(
            50m,
            cart.TotalAmount);
    }

    [Fact]
    public void AddDifferentVariant_CreatesSeparateLine()
    {
        var cart =
            CreateCart();

        var productId =
            ProductId.New();

        cart.AddItem(
            productId,
            ProductVariantId.New(),
            Money.Create(
                10m,
                "USD"),
            1,
            DateTimeOffset.UtcNow);

        cart.AddItem(
            productId,
            ProductVariantId.New(),
            Money.Create(
                15m,
                "USD"),
            2,
            DateTimeOffset.UtcNow);

        Assert.Equal(
            2,
            cart.Items.Count);

        Assert.Equal(
            3,
            cart.TotalQuantity);

        Assert.Equal(
            40m,
            cart.TotalAmount);
    }

    [Fact]
    public void AddItem_WithDifferentCurrency_Throws()
    {
        var cart =
            CreateCart();

        cart.AddItem(
            ProductId.New(),
            ProductVariantId.New(),
            Money.Create(
                10m,
                "USD"),
            1,
            DateTimeOffset.UtcNow);

        Assert.Throws<
            InvalidOperationException>(
                () =>
                    cart.AddItem(
                        ProductId.New(),
                        ProductVariantId.New(),
                        Money.Create(
                            20m,
                            "SAR"),
                        1,
                        DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AddItem_AboveMaximumQuantity_Throws()
    {
        var cart =
            CreateCart();

        Assert.Throws<
            ArgumentOutOfRangeException>(
                () =>
                    cart.AddItem(
                        ProductId.New(),
                        ProductVariantId.New(),
                        Money.Create(
                            10m,
                            "USD"),
                        CartItem.MaximumQuantity + 1,
                        DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ChangeItemQuantity_UpdatesTotals()
    {
        var cart =
            CreateCart();

        var item =
            cart.AddItem(
                ProductId.New(),
                ProductVariantId.New(),
                Money.Create(
                    12m,
                    "USD"),
                1,
                DateTimeOffset.UtcNow);

        cart.ChangeItemQuantity(
            item.Id,
            4,
            DateTimeOffset.UtcNow);

        Assert.Equal(
            4,
            cart.TotalQuantity);

        Assert.Equal(
            48m,
            cart.TotalAmount);
    }

    [Fact]
    public void RemoveLastItem_ResetsCurrency()
    {
        var cart =
            CreateCart();

        var item =
            cart.AddItem(
                ProductId.New(),
                ProductVariantId.New(),
                Money.Create(
                    10m,
                    "USD"),
                1,
                DateTimeOffset.UtcNow);

        cart.RemoveItem(
            item.Id,
            DateTimeOffset.UtcNow);

        Assert.Empty(
            cart.Items);

        Assert.Null(
            cart.Currency);

        Assert.Equal(
            0m,
            cart.TotalAmount);
    }

    [Fact]
    public void MarkConverted_WithItems_LocksCart()
    {
        var cart =
            CreateCart();

        cart.AddItem(
            ProductId.New(),
            ProductVariantId.New(),
            Money.Create(
                10m,
                "USD"),
            1,
            DateTimeOffset.UtcNow);

        cart.MarkConverted(
            DateTimeOffset.UtcNow);

        Assert.Equal(
            CartStatus.Converted,
            cart.Status);

        Assert.Throws<
            InvalidOperationException>(
                () =>
                    cart.AddItem(
                        ProductId.New(),
                        ProductVariantId.New(),
                        Money.Create(
                            10m,
                            "USD"),
                        1,
                        DateTimeOffset.UtcNow));
    }

    [Fact]
    public void EmptyCart_CannotBeConverted()
    {
        var cart =
            CreateCart();

        Assert.Throws<
            InvalidOperationException>(
                () =>
                    cart.MarkConverted(
                        DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RefreshItemPrice_UpdatesCartTotal()
    {
        var cart =
            CreateCart();

        var item =
            cart.AddItem(
                ProductId.New(),
                ProductVariantId.New(),
                Money.Create(
                    10m,
                    "USD"),
                2,
                DateTimeOffset.UtcNow);

        cart.RefreshItemPrice(
            item.Id,
            Money.Create(
                15m,
                "USD"),
            DateTimeOffset.UtcNow);

        Assert.Equal(
            30m,
            cart.TotalAmount);
    }

    private static Cart CreateCart()
    {
        return Cart.Create(
            TenantId.New(),
            UserId.New(),
            DateTimeOffset.UtcNow);
    }
}