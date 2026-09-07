using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Common.Payments;

public sealed record PaymentWebhookResult(
    bool IsAuthentic,
    string ExternalEventId,
    PaymentIntentStatus Status,
    string? ProviderReference = null,
    PaymentIntentId? PaymentIntentId = null,
    decimal? Amount = null,
    string? Currency = null,
    PaymentProviderAction? Action = null);
