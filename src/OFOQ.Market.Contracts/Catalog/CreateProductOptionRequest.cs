namespace OFOQ.Market.Contracts.Catalog;

public sealed record CreateProductOptionRequest(
    string Name,
    int SortOrder = 0);