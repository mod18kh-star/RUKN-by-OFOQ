using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Categories.CreateCategory;

public sealed class CategoryParentNotFoundException :
    Exception
{
    public CategoryParentNotFoundException(
        CategoryId parentCategoryId)
        : base(
            "The requested parent category was not found.")
    {
        ParentCategoryId =
            parentCategoryId;
    }

    public CategoryId ParentCategoryId { get; }
}