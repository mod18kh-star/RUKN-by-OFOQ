using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Payments.RetryIntent;

public sealed record RetryPaymentIntentCommand(
    PaymentIntentId PaymentIntentId,
    UserId CustomerUserId,
    string IdempotencyKey);
