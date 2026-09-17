namespace OFOQ.Market.Application.Catalog.Categories;

public sealed class CategoryHierarchyCycleException :
    Exception
{
    public CategoryHierarchyCycleException()
        : base(
            "A category cannot be moved beneath itself or one of its descendants.")
    {
    }
}
