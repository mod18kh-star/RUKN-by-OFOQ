namespace OFOQ.Market.Application.Catalog.Categories.CreateCategory;

public sealed class CategorySlugAlreadyExistsException :
    Exception
{
    public CategorySlugAlreadyExistsException(
        string slug)
        : base(
            $"A category with slug '{slug}' already exists in this tenant.")
    {
        Slug =
            slug;
    }

    public string Slug { get; }
}