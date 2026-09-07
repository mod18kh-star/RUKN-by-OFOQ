using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Payments.ExecuteIntent;

public sealed record ExecutePaymentIntentCommand(
    PaymentIntentId PaymentIntentId,
    UserId CustomerUserId);
