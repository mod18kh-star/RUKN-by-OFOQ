using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.Variants;

public sealed class ProductVariantProductNotFoundException :
    Exception
{
    public ProductVariantProductNotFoundException(
        ProductId productId)
        : base(
            "The requested product was not found.")
    {
        ProductId = productId;
    }

    public ProductId ProductId { get; }
}