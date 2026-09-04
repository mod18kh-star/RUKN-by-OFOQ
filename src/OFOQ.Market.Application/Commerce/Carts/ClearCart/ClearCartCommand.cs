using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Carts.ClearCart;

public sealed record ClearCartCommand(
    UserId CustomerUserId);