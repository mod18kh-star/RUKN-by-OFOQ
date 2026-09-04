namespace OFOQ.Market.Application.Catalog.Products.Variants;

public sealed class ProductVariantInvalidOptionSelectionException :
    Exception
{
    public ProductVariantInvalidOptionSelectionException(
        string message)
        : base(message)
    {
    }
}