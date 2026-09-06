namespace OFOQ.Market.Contracts.Commerce.Payments;

public sealed record PaymentIntentResponse(
    Guid PaymentIntentId,
    Guid PaymentId,
    Guid OrderId,
    Guid TenantPaymentMethodId,
    string Status,
    string MethodType,
    string ProviderCode,
    decimal Amount,
    string Currency,
    string? ProviderReference,
    DateTimeOffset CreatedAtUtc,
    bool IdempotentReplay);
