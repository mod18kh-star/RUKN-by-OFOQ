namespace OFOQ.Market.Contracts.Catalog;

public sealed record CreateProductOptionValueRequest(
    string Value,
    int SortOrder = 0);