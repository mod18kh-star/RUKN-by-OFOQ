namespace OFOQ.Market.Domain.Catalog.Attributes;

public readonly record struct ProductAttributeValueId(
    Guid Value)
{
    public static ProductAttributeValueId New() =>
        new(
            Guid.NewGuid());

    public static ProductAttributeValueId From(
        Guid value)
    {
        if (value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Product attribute value ID cannot be empty.",
                nameof(value));
        }

        return new ProductAttributeValueId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;
}