using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Commerce.Fulfillment;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed record CheckoutCommand(
    UserId CustomerUserId,
    string IdempotencyKey,
    CustomerAddressId? CustomerAddressId = null,
    ShippingMethodId? ShippingMethodId = null,
    string? CouponCode = null);
