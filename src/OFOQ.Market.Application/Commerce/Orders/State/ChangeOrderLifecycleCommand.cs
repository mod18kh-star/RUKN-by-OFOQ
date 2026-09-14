using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Orders.State;

public sealed record ChangeOrderLifecycleCommand(
    OrderId OrderId,
    OrderLifecycleAction Action,
    UserId ActorUserId,
    string? ShippingCarrier = null,
    string? TrackingNumber = null);