using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Payments;

public sealed record PaymentProviderRequest(
    TenantId TenantId,
    PaymentId PaymentId,
    PaymentIntentId PaymentIntentId,
    OrderId OrderId,
    UserId CustomerUserId,
    TenantPaymentMethodId TenantPaymentMethodId,
    Money Amount);
