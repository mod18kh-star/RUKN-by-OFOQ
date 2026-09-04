namespace OFOQ.Market.Application.Catalog.Products.CreateProduct;

public sealed class ProductSkuAlreadyExistsException :
    Exception
{
    public ProductSkuAlreadyExistsException(
        string sku)
        : base(
            $"A product variant with SKU '{sku}' already exists in this tenant.")
    {
        Sku = sku;
    }

    public string Sku { get; }
}