namespace OFOQ.Market.Application.Catalog.Products.Options;

public sealed class ProductOptionValueAlreadyExistsException :
    Exception
{
    public ProductOptionValueAlreadyExistsException(
        string value)
        : base(
            $"The option value '{value}' already exists.")
    {
        Value = value;
    }

    public string Value { get; }
}