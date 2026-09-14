using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Orders.Cancel;

public sealed record CancelOrderCommand(
    OrderId OrderId,
    string? Reason,
    UserId ActorUserId);