namespace OFOQ.Market.Contracts.Catalog;

public sealed record SetProductAttributeValueRequest(
    string Key,
    string? Value);

public sealed record SetProductAttributesRequest(
    IReadOnlyCollection<SetProductAttributeValueRequest> Values);