using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed record CheckoutCommand(
    UserId CustomerUserId,
    string IdempotencyKey);
