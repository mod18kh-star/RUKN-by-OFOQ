namespace OFOQ.Market.Domain.Catalog;

public readonly record struct CategoryId(
    Guid Value)
{
    public static CategoryId New()
    {
        return new CategoryId(
            Guid.NewGuid());
    }

    public static CategoryId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Category ID cannot be empty.",
                nameof(value));
        }

        return new CategoryId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}