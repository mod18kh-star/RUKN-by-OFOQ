using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Domain.Commerce.Orders;

public sealed partial class Order
{
    private readonly List<InventoryMovement>
        _inventoryMovements =
        [];

    public IReadOnlyCollection<InventoryMovement>
        InventoryMovements =>
        _inventoryMovements.AsReadOnly();

    public void RecordCheckoutInventoryDeduction(
        ProductId productId,
        ProductVariantId productVariantId,
        int quantityBefore,
        int quantityAfter,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        if (Status !=
            OrderStatus.Pending)
        {
            throw new InvalidOperationException(
                "Checkout inventory deduction can only be recorded for a pending order.");
        }

        if (quantityAfter >
            quantityBefore)
        {
            throw new ArgumentException(
                "Checkout inventory deduction cannot increase inventory.");
        }

        if (quantityAfter ==
            quantityBefore)
        {
            return;
        }

        if (_inventoryMovements.Any(
                movement =>
                    movement.Type ==
                    InventoryMovementType.CheckoutDeduction &&
                    movement.ProductVariantId ==
                    productVariantId))
        {
            throw new InvalidOperationException(
                "Checkout inventory deduction was already recorded for this product variant.");
        }

        _inventoryMovements.Add(
            InventoryMovement.Create(
                TenantId,
                Id,
                productId,
                productVariantId,
                InventoryMovementType.CheckoutDeduction,
                quantityBefore,
                quantityAfter,
                createdAtUtc,
                createdByUserId));
    }

    public int GetCancellationRestockQuantity(
        ProductVariantId productVariantId)
    {
        var deducted =
            _inventoryMovements
                .Where(
                    movement =>
                        movement.ProductVariantId ==
                        productVariantId &&
                        movement.Type ==
                        InventoryMovementType.CheckoutDeduction &&
                        movement.QuantityDelta <
                        0)
                .Sum(
                    movement =>
                        -movement.QuantityDelta);

        var alreadyRestocked =
            _inventoryMovements
                .Where(
                    movement =>
                        movement.ProductVariantId ==
                        productVariantId &&
                        movement.Type ==
                        InventoryMovementType.OrderCancellationRestock &&
                        movement.QuantityDelta >
                        0)
                .Sum(
                    movement =>
                        movement.QuantityDelta);

        return Math.Max(
            0,
            deducted -
            alreadyRestocked);
    }

    public void RecordCancellationInventoryRestock(
        ProductId productId,
        ProductVariantId productVariantId,
        int quantityBefore,
        int quantityAfter,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        if (Status !=
            OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Cancellation inventory restock requires a cancelled order.");
        }

        if (quantityAfter <=
            quantityBefore)
        {
            throw new ArgumentException(
                "Cancellation inventory restock must increase inventory.");
        }

        if (_inventoryMovements.Any(
                movement =>
                    movement.Type ==
                    InventoryMovementType.OrderCancellationRestock &&
                    movement.ProductVariantId ==
                    productVariantId))
        {
            throw new InvalidOperationException(
                "Cancellation inventory restock was already recorded for this product variant.");
        }

        _inventoryMovements.Add(
            InventoryMovement.Create(
                TenantId,
                Id,
                productId,
                productVariantId,
                InventoryMovementType.OrderCancellationRestock,
                quantityBefore,
                quantityAfter,
                createdAtUtc,
                createdByUserId));
    }
}