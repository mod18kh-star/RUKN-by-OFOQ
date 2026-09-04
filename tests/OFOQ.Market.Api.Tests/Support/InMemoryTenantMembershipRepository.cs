using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryTenantMembershipRepository :
    ITenantMembershipRepository
{
    private readonly object _syncRoot =
        new();

    private readonly List<TenantMembership> _items =
        [];

    public IReadOnlyList<TenantMembership> Items
    {
        get
        {
            lock (_syncRoot)
            {
                return _items
                    .ToArray();
            }
        }
    }

    public Task<TenantMembership?> GetByIdAsync(
        TenantMembershipId membershipId,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            var membership =
                _items.FirstOrDefault(
                    item =>
                        item.Id == membershipId &&
                        !item.IsDeleted);

            return Task.FromResult(
                membership);
        }
    }

    public Task<TenantMembership?> GetByTenantAndUserAsync(
        TenantId tenantId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            var membership =
                _items.FirstOrDefault(
                    item =>
                        item.TenantId == tenantId &&
                        item.UserId == userId &&
                        !item.IsDeleted);

            return Task.FromResult(
                membership);
        }
    }

    public Task<IReadOnlyList<TenantMembership>>
        GetByTenantIdAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            IReadOnlyList<TenantMembership> memberships =
                _items
                    .Where(
                        item =>
                            item.TenantId == tenantId &&
                            !item.IsDeleted)
                    .ToArray();

            return Task.FromResult(
                memberships);
        }
    }

    public Task<IReadOnlyList<TenantMembership>>
        GetByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            IReadOnlyList<TenantMembership> memberships =
                _items
                    .Where(
                        item =>
                            item.UserId == userId &&
                            !item.IsDeleted)
                    .ToArray();

            return Task.FromResult(
                memberships);
        }
    }

    public Task<bool> ExistsAsync(
        TenantId tenantId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            var exists =
                _items.Any(
                    item =>
                        item.TenantId == tenantId &&
                        item.UserId == userId &&
                        !item.IsDeleted);

            return Task.FromResult(
                exists);
        }
    }

    public Task AddAsync(
        TenantMembership membership,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            membership);

        lock (_syncRoot)
        {
            var duplicate =
                _items.Any(
                    item =>
                        item.TenantId == membership.TenantId &&
                        item.UserId == membership.UserId);

            if (duplicate)
            {
                throw new InvalidOperationException(
                    "A tenant membership for this user already exists.");
            }

            _items.Add(
                membership);
        }

        return Task.CompletedTask;
    }
}