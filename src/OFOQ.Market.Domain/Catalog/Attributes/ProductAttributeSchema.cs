using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Domain.Catalog.Attributes;

public sealed record ProductAttributeSchema
{
    public ProductAttributeSchema(
        CommerceVerticalType verticalType,
        IEnumerable<ProductAttributeDefinition> attributes)
    {
        if (verticalType ==
            CommerceVerticalType.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(verticalType));
        }

        ArgumentNullException.ThrowIfNull(
            attributes);

        VerticalType =
            verticalType;

        Attributes =
            attributes.ToArray();

        var duplicateKey =
            Attributes
                .GroupBy(
                    attribute =>
                        attribute.Key,
                    StringComparer.Ordinal)
                .FirstOrDefault(
                    group =>
                        group.Count() >
                        1);

        if (duplicateKey is not null)
        {
            throw new ArgumentException(
                $"Duplicate product attribute key '{duplicateKey.Key}'.",
                nameof(attributes));
        }
    }

    public CommerceVerticalType VerticalType { get; }

    public IReadOnlyCollection<ProductAttributeDefinition>
        Attributes { get; }

    public ProductAttributeDefinition? Find(
        string key)
    {
        if (string.IsNullOrWhiteSpace(
                key))
        {
            return null;
        }

        var normalized =
            key.Trim()
                .ToLowerInvariant();

        return Attributes.SingleOrDefault(
            attribute =>
                attribute.Key ==
                normalized);
    }
}