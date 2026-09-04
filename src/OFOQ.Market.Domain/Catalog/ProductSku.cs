namespace OFOQ.Market.Domain.Catalog;

public readonly record struct ProductSku
{
    private ProductSku(
        string value)
    {
        Value =
            value;
    }

    public string Value { get; }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(
            Value);

    public static ProductSku Create(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            throw new ArgumentException(
                "Product SKU is required.",
                nameof(value));
        }

        var normalized =
            value
                .Trim()
                .ToUpperInvariant();

        if (normalized.Length > 64)
        {
            throw new ArgumentException(
                "Product SKU cannot exceed 64 characters.",
                nameof(value));
        }

        foreach (var character in normalized)
        {
            var allowed =
                character is >= 'A' and <= 'Z'
                || character is >= '0' and <= '9'
                || character == '-'
                || character == '_'
                || character == '.';

            if (!allowed)
            {
                throw new ArgumentException(
                    "Product SKU may contain only English letters, numbers, hyphens, underscores and periods.",
                    nameof(value));
            }
        }

        return new ProductSku(
            normalized);
    }

    public override string ToString()
    {
        return Value;
    }
}