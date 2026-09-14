namespace OFOQ.Market.Application.Catalog.ProductAttributes;

public sealed record ProductAttributeFieldResult(
    string Key,
    string Label,
    string ValueType,
    IReadOnlyCollection<string> AllowedValues,
    string? Value);

public sealed record ProductAttributesResult(
    Guid ProductId,
    string Vertical,
    string VerticalCode,
    IReadOnlyList<ProductAttributeFieldResult> Attributes);