using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Payments.CreateIntent;

public sealed record CreatePaymentIntentCommand(
    OrderId OrderId,
    UserId CustomerUserId,
    TenantPaymentMethodId TenantPaymentMethodId,
    string IdempotencyKey);
