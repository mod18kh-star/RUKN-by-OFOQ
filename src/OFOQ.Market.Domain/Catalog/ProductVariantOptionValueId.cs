namespace OFOQ.Market.Domain.Catalog;

public readonly record struct ProductVariantOptionValueId(
    Guid Value)
{
    public static ProductVariantOptionValueId New()
    {
        return new ProductVariantOptionValueId(
            Guid.NewGuid());
    }

    public static ProductVariantOptionValueId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant option value ID cannot be empty.",
                nameof(value));
        }

        return new ProductVariantOptionValueId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}