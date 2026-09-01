using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryTenantRepository :
    ITenantRepository
{
    private readonly List<Tenant> _tenants = [];

    public Task<Tenant?> GetByIdAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant =
            _tenants.FirstOrDefault(
                item => item.Id == tenantId);

        return Task.FromResult(tenant);
    }

    public Task<Tenant?> GetBySlugAsync(
        TenantSlug slug,
        CancellationToken cancellationToken = default)
    {
        var tenant =
            _tenants.FirstOrDefault(
                item => item.Slug == slug);

        return Task.FromResult(tenant);
    }

    public Task<bool> SlugExistsAsync(
        TenantSlug slug,
        TenantId? excludingTenantId = null,
        CancellationToken cancellationToken = default)
    {
        var exists =
            _tenants.Any(
                tenant =>
                    tenant.Slug == slug &&
                    (!excludingTenantId.HasValue ||
                     tenant.Id != excludingTenantId.Value));

        return Task.FromResult(exists);
    }

    public Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        _tenants.Add(tenant);

        return Task.CompletedTask;
    }
}