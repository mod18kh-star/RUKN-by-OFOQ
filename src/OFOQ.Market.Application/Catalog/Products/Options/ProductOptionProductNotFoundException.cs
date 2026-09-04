using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.Options;

public sealed class ProductOptionProductNotFoundException :
    Exception
{
    public ProductOptionProductNotFoundException(
        ProductId productId)
        : base(
            "The requested product was not found.")
    {
        ProductId =
            productId;
    }

    public ProductId ProductId { get; }
}