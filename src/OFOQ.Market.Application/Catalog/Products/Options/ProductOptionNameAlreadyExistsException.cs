namespace OFOQ.Market.Application.Catalog.Products.Options;

public sealed class ProductOptionNameAlreadyExistsException :
    Exception
{
    public ProductOptionNameAlreadyExistsException(
        string name)
        : base(
            $"A product option named '{name}' already exists.")
    {
        Name =
            name;
    }

    public string Name { get; }
}