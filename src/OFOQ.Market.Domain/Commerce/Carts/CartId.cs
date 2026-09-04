namespace OFOQ.Market.Domain.Commerce.Carts;

public readonly record struct CartId(
    Guid Value)
{
    public static CartId New()
    {
        return new CartId(
            Guid.NewGuid());
    }

    public static CartId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Cart ID cannot be empty.",
                nameof(value));
        }

        return new CartId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}