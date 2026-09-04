using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Domain.Tests.Catalog;

public sealed class InventoryTests
{
    [Fact]
    public void Create_WithTrackedPositiveQuantity_IsAvailable()
    {
        var inventory =
            Inventory.Create(
                trackInventory: true,
                quantity: 10,
                lowStockThreshold: 2);

        Assert.True(
            inventory.IsAvailableForSale);

        Assert.False(
            inventory.IsOutOfStock);

        Assert.False(
            inventory.IsLowStock);
    }

    [Fact]
    public void Create_WithQuantityAtLowStockThreshold_IsLowStock()
    {
        var inventory =
            Inventory.Create(
                trackInventory: true,
                quantity: 2,
                lowStockThreshold: 2);

        Assert.True(
            inventory.IsLowStock);
    }

    [Fact]
    public void Create_WithZeroQuantity_IsOutOfStock()
    {
        var inventory =
            Inventory.Create(
                trackInventory: true,
                quantity: 0);

        Assert.True(
            inventory.IsOutOfStock);

        Assert.False(
            inventory.IsAvailableForSale);
    }

    [Fact]
    public void UntrackedInventory_IsAlwaysAvailable()
    {
        var inventory =
            Inventory.Create(
                trackInventory: false,
                quantity: 0);

        Assert.True(
            inventory.IsAvailableForSale);

        Assert.False(
            inventory.IsOutOfStock);
    }

    [Fact]
    public void Decrease_WithInsufficientStock_Throws()
    {
        var inventory =
            Inventory.Create(
                trackInventory: true,
                quantity: 2);

        Assert.Throws<
            InvalidOperationException>(
                () =>
                    inventory.Decrease(
                        3));
    }

    [Fact]
    public void IncreaseAndDecrease_ChangesQuantity()
    {
        var inventory =
            Inventory.Create(
                trackInventory: true,
                quantity: 5);

        inventory =
            inventory.Increase(
                3);

        Assert.Equal(
            8,
            inventory.Quantity);

        inventory =
            inventory.Decrease(
                2);

        Assert.Equal(
            6,
            inventory.Quantity);
    }

    [Fact]
    public void ContinueSellingWhenOutOfStock_AllowsAvailabilityAtZero()
    {
        var inventory =
            Inventory.Create(
                trackInventory: true,
                quantity: 0,
                continueSellingWhenOutOfStock: true);

        Assert.True(
            inventory.IsOutOfStock);

        Assert.True(
            inventory.IsAvailableForSale);
    }
}