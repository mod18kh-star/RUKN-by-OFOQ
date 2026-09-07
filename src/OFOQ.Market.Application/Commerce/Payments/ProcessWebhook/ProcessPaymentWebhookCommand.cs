using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.ProcessWebhook;

public sealed record ProcessPaymentWebhookCommand(
    TenantPaymentMethodId TenantPaymentMethodId,
    byte[] RawBody,
    IReadOnlyDictionary<string, string> Headers);
