namespace OFOQ.Market.Contracts.Catalog;

public sealed record ProductAttributeFieldResponse(
    string Key,
    string Label,
    string ValueType,
    IReadOnlyCollection<string> AllowedValues,
    string? Value);

public sealed record ProductAttributesResponse(
    Guid ProductId,
    string Vertical,
    string VerticalCode,
    IReadOnlyList<ProductAttributeFieldResponse> Attributes);