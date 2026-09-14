using System.Globalization;

namespace OFOQ.Market.Domain.Catalog.Attributes;

public static class ProductAttributeValueNormalizer
{
    public static string? Normalize(
        ProductAttributeDefinition definition,
        string? value)
    {
        ArgumentNullException.ThrowIfNull(
            definition);

        if (string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }

        var normalized =
            value.Trim();

        if (normalized.Length >
            500)
        {
            throw new ArgumentException(
                $"Product attribute '{definition.Key}' cannot exceed 500 characters.",
                nameof(value));
        }

        return definition.ValueType switch
        {
            ProductAttributeValueType.Text =>
                normalized,

            ProductAttributeValueType.Integer =>
                NormalizeInteger(
                    definition,
                    normalized),

            ProductAttributeValueType.Decimal =>
                NormalizeDecimal(
                    definition,
                    normalized),

            ProductAttributeValueType.Boolean =>
                NormalizeBoolean(
                    definition,
                    normalized),

            ProductAttributeValueType.Choice =>
                NormalizeChoice(
                    definition,
                    normalized),

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(definition))
        };
    }

    private static string NormalizeInteger(
        ProductAttributeDefinition definition,
        string value)
    {
        if (!long.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            throw InvalidValue(
                definition);
        }

        return parsed.ToString(
            CultureInfo.InvariantCulture);
    }

    private static string NormalizeDecimal(
        ProductAttributeDefinition definition,
        string value)
    {
        if (!decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            throw InvalidValue(
                definition);
        }

        return parsed.ToString(
            "0.############################",
            CultureInfo.InvariantCulture);
    }

    private static string NormalizeBoolean(
        ProductAttributeDefinition definition,
        string value)
    {
        if (!bool.TryParse(
                value,
                out var parsed))
        {
            throw InvalidValue(
                definition);
        }

        return parsed
            ? "true"
            : "false";
    }

    private static string NormalizeChoice(
        ProductAttributeDefinition definition,
        string value)
    {
        var allowed =
            definition.AllowedValues
                .SingleOrDefault(
                    option =>
                        string.Equals(
                            option,
                            value,
                            StringComparison.OrdinalIgnoreCase));

        if (allowed is null)
        {
            throw InvalidValue(
                definition);
        }

        return allowed;
    }

    private static ArgumentException InvalidValue(
        ProductAttributeDefinition definition)
    {
        return new ArgumentException(
            $"Invalid value for product attribute '{definition.Key}'.");
    }
}