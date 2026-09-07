using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryPaymentWebhookLockRepository :
    IPaymentWebhookLockRepository
{
    public Task AcquireAsync(
        TenantPaymentMethodId tenantPaymentMethodId,
        string externalEventId,
        CancellationToken cancellationToken = default)
    {
        if (tenantPaymentMethodId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant payment method ID cannot be empty.",
                nameof(tenantPaymentMethodId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(externalEventId);
        return Task.CompletedTask;
    }
}
