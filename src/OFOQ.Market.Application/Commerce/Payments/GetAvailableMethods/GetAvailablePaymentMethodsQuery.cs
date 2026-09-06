using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Payments.GetAvailableMethods;

public sealed record GetAvailablePaymentMethodsQuery(
    OrderId OrderId,
    UserId CustomerUserId);
