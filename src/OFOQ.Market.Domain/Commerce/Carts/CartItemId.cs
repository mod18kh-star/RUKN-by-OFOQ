namespace OFOQ.Market.Domain.Commerce.Carts;

public readonly record struct CartItemId(
    Guid Value)
{
    public static CartItemId New()
    {
        return new CartItemId(
            Guid.NewGuid());
    }

    public static CartItemId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Cart item ID cannot be empty.",
                nameof(value));
        }

        return new CartItemId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}