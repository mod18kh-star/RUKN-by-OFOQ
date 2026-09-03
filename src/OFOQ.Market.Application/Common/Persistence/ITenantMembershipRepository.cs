using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantMembershipRepository
{
    Task<TenantMembership?> GetByIdAsync(
        TenantMembershipId membershipId,
        CancellationToken cancellationToken = default);

    Task<TenantMembership?> GetByTenantAndUserAsync(
        TenantId tenantId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantMembership>> GetByTenantIdAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantMembership>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        TenantId tenantId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TenantMembership membership,
        CancellationToken cancellationToken = default);
}