namespace OFOQ.Market.Domain.Catalog;

public readonly record struct ProductOptionId(
    Guid Value)
{
    public static ProductOptionId New()
    {
        return new ProductOptionId(
            Guid.NewGuid());
    }

    public static ProductOptionId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Product option ID cannot be empty.",
                nameof(value));
        }

        return new ProductOptionId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}