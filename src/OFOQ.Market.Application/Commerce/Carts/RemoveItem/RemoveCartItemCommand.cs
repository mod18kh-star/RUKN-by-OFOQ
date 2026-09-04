using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Carts.RemoveItem;

public sealed record RemoveCartItemCommand(
    UserId CustomerUserId,
    CartItemId CartItemId);