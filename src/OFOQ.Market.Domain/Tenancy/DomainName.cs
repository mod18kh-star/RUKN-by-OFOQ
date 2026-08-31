using System.Globalization;

namespace OFOQ.Market.Domain.Tenancy;

public readonly record struct DomainName
{
    private DomainName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static DomainName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Domain name is required.",
                nameof(value));
        }

        value = value.Trim();

        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Domain name must not contain a URL scheme.",
                nameof(value));
        }

        if (value.Contains('/') ||
            value.Contains('\\') ||
            value.Contains(':'))
        {
            throw new ArgumentException(
                "Domain name must not contain paths or ports.",
                nameof(value));
        }

        value = value.TrimEnd('.');

        string asciiDomain;

        try
        {
            asciiDomain = new IdnMapping()
                .GetAscii(value)
                .ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            throw new ArgumentException(
                "Domain name is invalid.",
                nameof(value));
        }

        if (asciiDomain.Length > 253)
        {
            throw new ArgumentException(
                "Domain name cannot exceed 253 characters.",
                nameof(value));
        }

        var labels = asciiDomain.Split('.');

        if (labels.Length < 2)
        {
            throw new ArgumentException(
                "A custom domain must contain at least one dot.",
                nameof(value));
        }

        foreach (var label in labels)
        {
            ValidateLabel(label, value);
        }

        return new DomainName(asciiDomain);
    }

    public override string ToString() => Value;

    private static void ValidateLabel(
        string label,
        string originalValue)
    {
        if (string.IsNullOrWhiteSpace(label) ||
            label.Length > 63)
        {
            throw new ArgumentException(
                "Domain contains an invalid label.",
                nameof(originalValue));
        }

        if (!IsAsciiLetterOrDigit(label[0]) ||
            !IsAsciiLetterOrDigit(label[^1]))
        {
            throw new ArgumentException(
                "Domain labels must start and end with a letter or number.",
                nameof(originalValue));
        }

        foreach (var character in label)
        {
            if (!IsAsciiLetterOrDigit(character) &&
                character != '-')
            {
                throw new ArgumentException(
                    "Domain contains invalid characters.",
                    nameof(originalValue));
            }
        }
    }

    private static bool IsAsciiLetterOrDigit(char character)
        => character is >= 'a' and <= 'z'
           || character is >= '0' and <= '9';
}