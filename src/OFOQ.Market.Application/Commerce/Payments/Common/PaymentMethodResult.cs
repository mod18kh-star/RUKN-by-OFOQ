using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.Common;

public sealed record PaymentMethodResult(
    TenantPaymentMethodId Id,
    string MethodType,
    string ProviderCode,
    string DisplayName,
    string Country,
    string Currency,
    decimal? MinimumAmount,
    decimal? MaximumAmount);
