using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Payments;

public interface IPaymentProviderCredentialProtector
{
    string Protect(
        TenantId tenantId,
        TenantPaymentProviderAccountId accountId,
        PaymentProviderCredentialPayload payload);

    PaymentProviderCredentialPayload Unprotect(
        TenantId tenantId,
        TenantPaymentProviderAccountId accountId,
        string protectedPayload);
}