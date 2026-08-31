using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tests.Tenancy.CreateTenant;

internal sealed class FakeTenantRepository :
    ITenantRepository
{
    public bool SlugExists { get; set; }

    public Tenant? AddedTenant { get; private set; }

    public Task<Tenant?> GetByIdAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Tenant?>(null);
    }

    public Task<Tenant?> GetBySlugAsync(
        TenantSlug slug,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Tenant?>(null);
    }

    public Task<bool> SlugExistsAsync(
        TenantSlug slug,
        TenantId? excludingTenantId = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SlugExists);
    }

    public Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        AddedTenant = tenant;

        return Task.CompletedTask;
    }
}