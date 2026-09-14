namespace OFOQ.Market.Domain.Catalog.Attributes;

public sealed record ProductAttributeDefinition
{
    public ProductAttributeDefinition(
        string key,
        string label,
        ProductAttributeValueType valueType,
        IEnumerable<string>? allowedValues = null)
    {
        Key =
            NormalizeKey(
                key);

        if (string.IsNullOrWhiteSpace(
                label))
        {
            throw new ArgumentException(
                "Product attribute label is required.",
                nameof(label));
        }

        Label =
            label.Trim();

        ValueType =
            valueType;

        AllowedValues =
            allowedValues?
                .Where(
                    value =>
                        !string.IsNullOrWhiteSpace(
                            value))
                .Select(
                    value =>
                        value.Trim())
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray()
            ?? Array.Empty<string>();

        if (ValueType ==
                ProductAttributeValueType.Choice &&
            AllowedValues.Count ==
                0)
        {
            throw new ArgumentException(
                "Choice attributes require at least one allowed value.",
                nameof(allowedValues));
        }

        if (ValueType !=
                ProductAttributeValueType.Choice &&
            AllowedValues.Count >
                0)
        {
            throw new ArgumentException(
                "Allowed values are supported only for choice attributes.",
                nameof(allowedValues));
        }
    }

    public string Key { get; }

    public string Label { get; }

    public ProductAttributeValueType ValueType { get; }

    public IReadOnlyCollection<string> AllowedValues { get; }

    private static string NormalizeKey(
        string key)
    {
        if (string.IsNullOrWhiteSpace(
                key))
        {
            throw new ArgumentException(
                "Product attribute key is required.",
                nameof(key));
        }

        var normalized =
            key.Trim()
                .ToLowerInvariant();

        if (normalized.Length >
            80)
        {
            throw new ArgumentException(
                "Product attribute key cannot exceed 80 characters.",
                nameof(key));
        }

        return normalized;
    }
}