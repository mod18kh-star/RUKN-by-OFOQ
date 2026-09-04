namespace OFOQ.Market.Domain.Catalog;

public readonly record struct ProductOptionValueId(
    Guid Value)
{
    public static ProductOptionValueId New()
    {
        return new ProductOptionValueId(
            Guid.NewGuid());
    }

    public static ProductOptionValueId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Product option value ID cannot be empty.",
                nameof(value));
        }

        return new ProductOptionValueId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}