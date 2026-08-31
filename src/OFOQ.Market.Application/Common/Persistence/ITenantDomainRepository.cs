using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantDomainRepository
{
    Task<TenantDomain?> GetByIdAsync(
        TenantDomainId tenantDomainId,
        CancellationToken cancellationToken = default);

    Task<TenantDomain?> GetByDomainAsync(
        DomainName domain,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantDomain>> GetByTenantIdAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> DomainExistsAsync(
        DomainName domain,
        TenantDomainId? excludingTenantDomainId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TenantDomain tenantDomain,
        CancellationToken cancellationToken = default);
}