namespace OFOQ.Market.Domain.Commerce.Payments;

public readonly record struct PaymentProviderCode
{
    private PaymentProviderCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static PaymentProviderCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Payment provider code is required.",
                nameof(value));
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > 80)
        {
            throw new ArgumentException(
                "Payment provider code cannot exceed 80 characters.",
                nameof(value));
        }

        foreach (var character in normalized)
        {
            if (!char.IsLetterOrDigit(character) &&
                character is not '-' and not '_' and not '.')
            {
                throw new ArgumentException(
                    "Payment provider code contains invalid characters.",
                    nameof(value));
            }
        }

        return new PaymentProviderCode(normalized);
    }

    public override string ToString() => Value;
}
