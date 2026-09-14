namespace OFOQ.Market.Domain.Commerce.Orders;

public readonly record struct InventoryMovementId(
    Guid Value)
{
    public static InventoryMovementId New() =>
        new(
            Guid.NewGuid());

    public static InventoryMovementId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Inventory movement ID cannot be empty.",
                nameof(value));
        }

        return new InventoryMovementId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString() =>
        Value.ToString();
}