using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantPaymentCapabilityRepository
{
    Task<TenantPaymentCapability?> GetAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TenantPaymentCapability capability,
        CancellationToken cancellationToken = default);
}
