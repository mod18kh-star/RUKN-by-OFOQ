namespace OFOQ.Market.Domain.Catalog;

public readonly record struct CurrencyCode
{
    private CurrencyCode(
        string value)
    {
        Value =
            value;
    }

    public string Value { get; }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(
            Value);

    public static CurrencyCode Create(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            throw new ArgumentException(
                "Currency code is required.",
                nameof(value));
        }

        var normalized =
            value
                .Trim()
                .ToUpperInvariant();

        if (normalized.Length != 3)
        {
            throw new ArgumentException(
                "Currency code must contain exactly three letters.",
                nameof(value));
        }

        foreach (var character in normalized)
        {
            if (character is < 'A' or > 'Z')
            {
                throw new ArgumentException(
                    "Currency code must contain only English letters.",
                    nameof(value));
            }
        }

        return new CurrencyCode(
            normalized);
    }

    public override string ToString()
    {
        return Value;
    }
}