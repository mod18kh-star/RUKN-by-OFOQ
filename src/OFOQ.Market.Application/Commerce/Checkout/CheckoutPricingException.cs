namespace OFOQ.Market.Application.Commerce.Checkout;
public sealed class CheckoutPricingException : InvalidOperationException
{
    public CheckoutPricingException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}
