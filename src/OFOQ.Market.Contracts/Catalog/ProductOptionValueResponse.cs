namespace OFOQ.Market.Contracts.Catalog;

public sealed record ProductOptionValueResponse(
    Guid ValueId,
    string Value,
    int SortOrder);