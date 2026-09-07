namespace OFOQ.Market.Application.Common.Payments;

public sealed class PaymentProviderCredentialPayload
{
    private const int MaximumCredentialCount = 128;
    private const int MaximumCredentialKeyLength = 100;
    private const int MaximumCredentialValueLength = 65536;

    private PaymentProviderCredentialPayload(
        IReadOnlyDictionary<string, string> values)
    {
        Values = values;
    }

    public IReadOnlyDictionary<string, string> Values { get; }

    public static PaymentProviderCredentialPayload Create(
        IReadOnlyDictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(
            credentials);

        if (credentials.Count == 0)
        {
            throw new ArgumentException(
                "At least one payment provider credential is required.",
                nameof(credentials));
        }

        if (credentials.Count >
            MaximumCredentialCount)
        {
            throw new ArgumentException(
                $"Payment provider credentials cannot exceed {MaximumCredentialCount} entries.",
                nameof(credentials));
        }

        var normalized =
            new Dictionary<string, string>(
                StringComparer.Ordinal);

        foreach (var pair in credentials)
        {
            if (string.IsNullOrWhiteSpace(
                    pair.Key))
            {
                throw new ArgumentException(
                    "Payment provider credential keys cannot be empty.",
                    nameof(credentials));
            }

            var key =
                pair.Key.Trim();

            if (key.Length >
                MaximumCredentialKeyLength)
            {
                throw new ArgumentException(
                    $"Payment provider credential keys cannot exceed {MaximumCredentialKeyLength} characters.",
                    nameof(credentials));
            }

            if (pair.Value is null ||
                pair.Value.Length == 0)
            {
                throw new ArgumentException(
                    $"Payment provider credential '{key}' cannot be empty.",
                    nameof(credentials));
            }

            if (pair.Value.Length >
                MaximumCredentialValueLength)
            {
                throw new ArgumentException(
                    $"Payment provider credential '{key}' cannot exceed {MaximumCredentialValueLength} characters.",
                    nameof(credentials));
            }

            if (!normalized.TryAdd(
                    key,
                    pair.Value))
            {
                throw new ArgumentException(
                    $"Duplicate payment provider credential key '{key}'.",
                    nameof(credentials));
            }
        }

        return new PaymentProviderCredentialPayload(
            normalized);
    }

    public override string ToString()
    {
        return "[PAYMENT_PROVIDER_CREDENTIALS_REDACTED]";
    }
}