using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.Variants;

public sealed record ProductVariantSelectionResult(
    ProductOptionId OptionId,
    string OptionName,
    ProductOptionValueId ValueId,
    string Value);