namespace OFOQ.Market.Application.Catalog.ProductAttributes;

public sealed class ProductAttributesVerticalNotConfiguredException :
    InvalidOperationException
{
    public ProductAttributesVerticalNotConfiguredException()
        : base(
            "A primary commerce vertical must be configured before product attributes can be used.")
    {
    }
}