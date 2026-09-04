namespace OFOQ.Market.Domain.Catalog;

public readonly record struct ProductVariantId(
    Guid Value)
{
    public static ProductVariantId New()
    {
        return new ProductVariantId(
            Guid.NewGuid());
    }

    public static ProductVariantId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant ID cannot be empty.",
                nameof(value));
        }

        return new ProductVariantId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}