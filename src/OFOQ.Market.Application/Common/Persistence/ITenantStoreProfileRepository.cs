using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantStoreProfileRepository
{
    Task<TenantStoreProfile?> GetAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TenantStoreProfile profile,
        CancellationToken cancellationToken = default);
}
