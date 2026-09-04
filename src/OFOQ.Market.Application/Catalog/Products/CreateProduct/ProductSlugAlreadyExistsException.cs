namespace OFOQ.Market.Application.Catalog.Products.CreateProduct;

public sealed class ProductSlugAlreadyExistsException :
    Exception
{
    public ProductSlugAlreadyExistsException(
        string slug)
        : base(
            $"A product with slug '{slug}' already exists in this tenant.")
    {
        Slug = slug;
    }

    public string Slug { get; }
}