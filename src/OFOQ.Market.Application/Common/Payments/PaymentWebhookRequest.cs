using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Payments;

public sealed record PaymentWebhookRequest(
    TenantId TenantId,
    TenantPaymentMethodId TenantPaymentMethodId,
    byte[] RawBody,
    IReadOnlyDictionary<string, string> Headers);
