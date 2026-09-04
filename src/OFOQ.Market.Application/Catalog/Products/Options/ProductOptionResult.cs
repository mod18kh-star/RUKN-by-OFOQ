using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.Options;

public sealed record ProductOptionResult(
    ProductOptionId OptionId,
    string Name,
    int SortOrder,
    IReadOnlyList<ProductOptionValueResult> Values);