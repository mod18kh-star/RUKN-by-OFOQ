using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Payments.GetIntent;

public sealed record GetPaymentIntentQuery(
    PaymentIntentId PaymentIntentId,
    UserId CustomerUserId);
