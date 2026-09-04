namespace OFOQ.Market.Domain.Catalog;

public readonly record struct ProductId(
    Guid Value)
{
    public static ProductId New()
    {
        return new ProductId(
            Guid.NewGuid());
    }

    public static ProductId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Product ID cannot be empty.",
                nameof(value));
        }

        return new ProductId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}