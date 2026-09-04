using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Carts.UpdateItemQuantity;

public sealed record UpdateCartItemQuantityCommand(
    UserId CustomerUserId,
    CartItemId CartItemId,
    int Quantity);