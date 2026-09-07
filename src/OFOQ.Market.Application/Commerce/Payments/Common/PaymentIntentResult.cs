using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed record PaymentIntentResult(
    PaymentIntentId PaymentIntentId,
    PaymentId PaymentId,
    OrderId OrderId,
    TenantPaymentMethodId TenantPaymentMethodId,
    string Status,
    string MethodType,
    string ProviderCode,
    decimal Amount,
    string Currency,
    string? ProviderReference,
    DateTimeOffset CreatedAtUtc,
    bool IsIdempotentReplay)
{
    public PaymentIntentActionType? ActionType { get; init; }

    public string? ActionValue { get; init; }
}