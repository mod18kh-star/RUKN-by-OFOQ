namespace OFOQ.Market.Contracts.Catalog;

public sealed record ProductOptionResponse(
    Guid OptionId,
    string Name,
    int SortOrder,
    IReadOnlyList<ProductOptionValueResponse> Values);