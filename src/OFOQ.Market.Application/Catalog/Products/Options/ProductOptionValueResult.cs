using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.Options;

public sealed record ProductOptionValueResult(
    ProductOptionValueId ValueId,
    string Value,
    int SortOrder);