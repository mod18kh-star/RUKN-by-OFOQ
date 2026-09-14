namespace OFOQ.Market.Domain.Catalog;

public sealed partial class ProductVariant
{
    public void RestoreStockFromOrderCancellation(
        int quantity,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Inventory restore quantity must be greater than zero.");
        }

        ApplyInventory(
            Inventory.Increase(
                quantity));

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }
}