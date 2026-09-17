namespace OFOQ.Market.Domain.Catalog;

public readonly record struct ProductContentBlockId(
    Guid Value)
{
    public static ProductContentBlockId New() =>
        new(
            Guid.NewGuid());

    public static ProductContentBlockId From(
        Guid value)
    {
        if (value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Product content block ID cannot be empty.",
                nameof(value));
        }

        return new ProductContentBlockId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;
}
