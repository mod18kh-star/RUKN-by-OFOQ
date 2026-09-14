namespace OFOQ.Market.Domain.Catalog;

public readonly record struct ProductImageId(
    Guid Value)
{
    public static ProductImageId New() =>
        new(
            Guid.NewGuid());

    public static ProductImageId From(
        Guid value)
    {
        if (value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Product image ID cannot be empty.",
                nameof(value));
        }

        return new ProductImageId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;
}