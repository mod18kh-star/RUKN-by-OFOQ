namespace OFOQ.Market.Domain.Commerce.Payments;

public readonly record struct CountryCode
{
    private CountryCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static CountryCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Country code is required.",
                nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length != 2 ||
            normalized.Any(character =>
                character is < 'A' or > 'Z'))
        {
            throw new ArgumentException(
                "Country code must be a two-letter ISO-style code.",
                nameof(value));
        }

        return new CountryCode(normalized);
    }

    public override string ToString() => Value;
}
