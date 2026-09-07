namespace OFOQ.Market.Contracts.Commerce.Payments;

public sealed record PaymentWebhookResponse(
    Guid PaymentIntentId,
    Guid PaymentId,
    Guid OrderId,
    string Status,
    bool Duplicate,
    bool Applied);
