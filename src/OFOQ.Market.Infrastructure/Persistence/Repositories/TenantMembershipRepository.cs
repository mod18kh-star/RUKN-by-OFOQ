using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class TenantMembershipRepository :
    ITenantMembershipRepository
{
    private readonly MarketDbContext _dbContext;

    public TenantMembershipRepository(
        MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TenantMembership?> GetByIdAsync(
        TenantMembershipId membershipId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantMemberships
            .FirstOrDefaultAsync(
                membership =>
                    membership.Id == membershipId,
                cancellationToken);
    }

    public Task<TenantMembership?> GetByTenantAndUserAsync(
        TenantId tenantId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantMemberships
            .FirstOrDefaultAsync(
                membership =>
                    membership.TenantId == tenantId &&
                    membership.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMembership>>
        GetByTenantIdAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantMemberships
            .Where(
                membership =>
                    membership.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMembership>>
        GetByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantMemberships
            .Where(
                membership =>
                    membership.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(
        TenantId tenantId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .AnyAsync(
                membership =>
                    membership.TenantId == tenantId &&
                    membership.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(
        TenantMembership membership,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(membership);

        await _dbContext.TenantMemberships.AddAsync(
            membership,
            cancellationToken);
    }
}