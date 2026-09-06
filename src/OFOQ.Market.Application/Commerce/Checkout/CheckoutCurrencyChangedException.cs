namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed class CheckoutCurrencyChangedException :
    InvalidOperationException
{
    public CheckoutCurrencyChangedException()
        : base(
            "The authoritative catalog prices in this cart no longer use a single currency.")
    {
    }
}
