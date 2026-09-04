namespace OFOQ.Market.Application.Catalog.Products.Variants;

public sealed class ProductVariantCombinationAlreadyExistsException :
    Exception
{
    public ProductVariantCombinationAlreadyExistsException()
        : base(
            "A variant with the same option combination already exists.")
    {
    }
}