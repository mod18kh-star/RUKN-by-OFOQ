using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);

    Task<Tenant?> GetBySlugAsync(
        TenantSlug slug,
        CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(
        TenantSlug slug,
        TenantId? excludingTenantId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken = default);
}