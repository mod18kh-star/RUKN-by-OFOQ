using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Common.Payments;

public sealed record PaymentProviderResult(
    PaymentIntentStatus Status,
    string? ProviderReference,
    PaymentProviderAction? Action = null);
