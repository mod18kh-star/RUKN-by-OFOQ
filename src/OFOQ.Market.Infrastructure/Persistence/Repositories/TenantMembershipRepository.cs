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
        _dbContext =
            dbContext;
    }

    public Task<TenantMembership?> GetByIdAsync(
        TenantMembershipId membershipId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .TenantMemberships
            .FirstOrDefaultAsync(
                membership =>
                    membership.Id ==
                    membershipId,
                cancellationToken);
    }

    public Task<TenantMembership?> GetByTenantAndUserAsync(
        TenantId tenantId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .TenantMemberships
            .FirstOrDefaultAsync(
                membership =>
                    membership.TenantId ==
                        tenantId &&
                    membership.UserId ==
                        userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMembership>>
        GetByTenantIdAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .TenantMemberships
            .Where(
                membership =>
                    membership.TenantId ==
                    tenantId)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMembership>>
        GetByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
    {
        /*
         * This is an authenticated user-level discovery query.
         *
         * There is intentionally no tenant route at this point:
         * the purpose of this query is to discover which tenants
         * the authenticated user belongs to.
         *
         * Tenant query filters are therefore bypassed here only.
         * The query remains strictly constrained to the exact
         * authenticated UserId and excludes deleted memberships.
         */
        return await _dbContext
            .Set<TenantMembership>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(
                membership =>
                    membership.UserId == userId &&
                    !membership.IsDeleted)
            .OrderByDescending(
                membership =>
                    membership.CreatedAtUtc)
            .ToListAsync(
                cancellationToken);
    }

    public Task<bool> ExistsAsync(
        TenantId tenantId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        /*
         * Do NOT IgnoreQueryFilters here.
         *
         * A soft-deleted membership must behave as if it does
         * not exist for authorization/business operations.
         */
        return _dbContext
            .TenantMemberships
            .AnyAsync(
                membership =>
                    membership.TenantId ==
                        tenantId &&
                    membership.UserId ==
                        userId,
                cancellationToken);
    }

    public async Task AddAsync(
        TenantMembership membership,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            membership);

        await _dbContext
            .TenantMemberships
            .AddAsync(
                membership,
                cancellationToken);
    }
}