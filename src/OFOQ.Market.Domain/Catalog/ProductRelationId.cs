namespace OFOQ.Market.Domain.Catalog;

public readonly record struct ProductRelationId(
    Guid Value)
{
    public static ProductRelationId New() =>
        new(
            Guid.NewGuid());

    public static ProductRelationId From(
        Guid value)
    {
        if (value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Product relation ID cannot be empty.",
                nameof(value));
        }

        return new ProductRelationId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;
}
