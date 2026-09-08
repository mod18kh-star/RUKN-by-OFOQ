using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class TenantCommerceCapabilityOverrideRepository :
    ITenantCommerceCapabilityOverrideRepository
{
    private readonly MarketDbContext
        _dbContext;

    public TenantCommerceCapabilityOverrideRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<TenantCommerceCapabilityOverride>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<TenantCommerceCapabilityOverride>()
            .OrderBy(
                capabilityOverride =>
                    capabilityOverride.CapabilityType)
            .ToArrayAsync(
                cancellationToken);
    }

    public Task<TenantCommerceCapabilityOverride?> GetByCapabilityAsync(
        CommerceCapabilityType capabilityType,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Set<TenantCommerceCapabilityOverride>()
            .SingleOrDefaultAsync(
                capabilityOverride =>
                    capabilityOverride.CapabilityType ==
                    capabilityType,
                cancellationToken);
    }

    public async Task AddAsync(
        TenantCommerceCapabilityOverride capabilityOverride,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            capabilityOverride);

        await _dbContext
            .Set<TenantCommerceCapabilityOverride>()
            .AddAsync(
                capabilityOverride,
                cancellationToken);
    }

    public void Remove(
        TenantCommerceCapabilityOverride capabilityOverride)
    {
        ArgumentNullException.ThrowIfNull(
            capabilityOverride);

        _dbContext
            .Set<TenantCommerceCapabilityOverride>()
            .Remove(
                capabilityOverride);
    }
}