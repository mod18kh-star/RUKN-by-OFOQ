namespace OFOQ.Market.Contracts.Commerce.Checkout;
public sealed record CheckoutRequest(Guid? CustomerAddressId, Guid? ShippingMethodId, string? CouponCode);
