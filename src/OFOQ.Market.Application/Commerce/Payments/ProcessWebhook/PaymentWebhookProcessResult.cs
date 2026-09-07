using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.ProcessWebhook;

public sealed record PaymentWebhookProcessResult(
    PaymentIntentId PaymentIntentId,
    PaymentId PaymentId,
    OrderId OrderId,
    string Status,
    bool Duplicate,
    bool Applied);
