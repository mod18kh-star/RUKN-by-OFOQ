using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.Variants;

public sealed class ProductVariantOptionValueNotFoundException :
    Exception
{
    public ProductVariantOptionValueNotFoundException(
        ProductOptionValueId valueId)
        : base(
            "One of the selected product option values was not found.")
    {
        ValueId = valueId;
    }

    public ProductOptionValueId ValueId { get; }
}