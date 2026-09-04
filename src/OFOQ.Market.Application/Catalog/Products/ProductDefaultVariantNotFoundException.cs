using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products;

public sealed class ProductDefaultVariantNotFoundException :
    Exception
{
    public ProductDefaultVariantNotFoundException(
        ProductId productId)
        : base(
            $"The default variant for product '{productId.Value}' was not found.")
    {
        ProductId =
            productId;
    }

    public ProductId ProductId { get; }
}