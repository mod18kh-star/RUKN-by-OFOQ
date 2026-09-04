using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.Products.Options;

public sealed class ProductOptionNotFoundException :
    Exception
{
    public ProductOptionNotFoundException(
        ProductOptionId optionId)
        : base(
            "The requested product option was not found.")
    {
        OptionId =
            optionId;
    }

    public ProductOptionId OptionId { get; }
}