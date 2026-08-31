using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository : ITenantRepository
{
    private readonly MarketDbContext _dbContext;

    public TenantRepository(
        MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Tenant?> GetByIdAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Tenants
            .FirstOrDefaultAsync(
                tenant => tenant.Id == tenantId,
                cancellationToken);
    }

    public Task<Tenant?> GetBySlugAsync(
        TenantSlug slug,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Tenants
            .FirstOrDefaultAsync(
                tenant => tenant.Slug == slug,
                cancellationToken);
    }

    public Task<bool> SlugExistsAsync(
        TenantSlug slug,
        TenantId? excludingTenantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(tenant => tenant.Slug == slug);

        if (excludingTenantId.HasValue)
        {
            var tenantId = excludingTenantId.Value;

            query = query.Where(
                tenant => tenant.Id != tenantId);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        await _dbContext.Tenants.AddAsync(
            tenant,
            cancellationToken);
    }
}