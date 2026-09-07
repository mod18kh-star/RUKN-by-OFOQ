using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IPaymentWebhookLockRepository
{
    Task AcquireAsync(
        TenantPaymentMethodId tenantPaymentMethodId,
        string externalEventId,
        CancellationToken cancellationToken = default);
}
