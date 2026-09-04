namespace OFOQ.Market.Application.Commerce.Carts;

public sealed class StructuredVariantRequiredException :
    Exception
{
    public StructuredVariantRequiredException()
        : base(
            "A complete structured product variant must be selected.")
    {
    }
}