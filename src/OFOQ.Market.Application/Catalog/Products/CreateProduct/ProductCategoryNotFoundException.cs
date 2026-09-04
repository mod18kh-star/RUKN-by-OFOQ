using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.CreateProduct;

public sealed class ProductCategoryNotFoundException :
    Exception
{
    public ProductCategoryNotFoundException(
        CategoryId categoryId)
        : base(
            "The requested product category was not found.")
    {
        CategoryId = categoryId;
    }

    public CategoryId CategoryId { get; }
}