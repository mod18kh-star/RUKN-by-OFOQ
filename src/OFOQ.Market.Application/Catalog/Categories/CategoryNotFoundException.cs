using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Categories;

public sealed class CategoryNotFoundException :
    Exception
{
    public CategoryNotFoundException(
        CategoryId categoryId)
        : base(
            $"Category '{categoryId.Value}' was not found.")
    {
        CategoryId =
            categoryId;
    }

    public CategoryId CategoryId { get; }
}
