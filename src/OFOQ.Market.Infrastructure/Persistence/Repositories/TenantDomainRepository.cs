using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class TenantDomainRepository :
    ITenantDomainRepository
{
    private readonly MarketDbContext _dbContext;

    public TenantDomainRepository(
        MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TenantDomain?> GetByIdAsync(
        TenantDomainId tenantDomainId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantDomains
            .FirstOrDefaultAsync(
                domain => domain.Id == tenantDomainId,
                cancellationToken);
    }

    public Task<TenantDomain?> GetByDomainAsync(
        DomainName domain,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantDomains
            .FirstOrDefaultAsync(
                tenantDomain => tenantDomain.Domain == domain,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TenantDomain>> GetByTenantIdAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantDomains
            .Where(
                domain => domain.TenantId == tenantId)
            .OrderByDescending(
                domain => domain.IsPrimary)
            .ThenBy(
                domain => domain.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> DomainExistsAsync(
        DomainName domain,
        TenantDomainId? excludingTenantDomainId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TenantDomains
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(
                tenantDomain => tenantDomain.Domain == domain);

        if (excludingTenantDomainId.HasValue)
        {
            var domainId =
                excludingTenantDomainId.Value;

            query = query.Where(
                tenantDomain => tenantDomain.Id != domainId);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(
        TenantDomain tenantDomain,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantDomain);

        await _dbContext.TenantDomains.AddAsync(
            tenantDomain,
            cancellationToken);
    }
}