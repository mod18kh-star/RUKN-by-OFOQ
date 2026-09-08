using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryTenantCommerceCapabilityOverrideStore
{
    public object SyncRoot { get; } =
        new();

    public List<TenantCommerceCapabilityOverride> Items { get; } =
        [];
}

internal sealed class InMemoryTenantCommerceCapabilityOverrideRepository :
    ITenantCommerceCapabilityOverrideRepository
{
    private readonly InMemoryTenantCommerceCapabilityOverrideStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryTenantCommerceCapabilityOverrideRepository(
        InMemoryTenantCommerceCapabilityOverrideStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<IReadOnlyList<TenantCommerceCapabilityOverride>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IReadOnlyList<TenantCommerceCapabilityOverride> result =
                _store.Items
                    .Where(
                        item =>
                            item.TenantId ==
                            tenantId)
                    .OrderBy(
                        item =>
                            item.CapabilityType)
                    .ToArray();

            return Task.FromResult(
                result);
        }
    }

    public Task<TenantCommerceCapabilityOverride?> GetByCapabilityAsync(
        CommerceCapabilityType capabilityType,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var capabilityOverride =
                _store.Items.SingleOrDefault(
                    item =>
                        item.TenantId ==
                            tenantId &&
                        item.CapabilityType ==
                            capabilityType);

            return Task.FromResult(
                capabilityOverride);
        }
    }

    public Task AddAsync(
        TenantCommerceCapabilityOverride capabilityOverride,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            capabilityOverride);

        var tenantId =
            GetRequiredTenantId();

        if (capabilityOverride.TenantId !=
            tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant commerce capability override creation was blocked.");
        }

        lock (_store.SyncRoot)
        {
            var duplicate =
                _store.Items.Any(
                    item =>
                        item.TenantId ==
                            tenantId &&
                        item.CapabilityType ==
                            capabilityOverride.CapabilityType);

            if (duplicate)
            {
                throw new InvalidOperationException(
                    "A capability override of this type already exists for the tenant.");
            }

            _store.Items.Add(
                capabilityOverride);
        }

        return Task.CompletedTask;
    }

    public void Remove(
        TenantCommerceCapabilityOverride capabilityOverride)
    {
        ArgumentNullException.ThrowIfNull(
            capabilityOverride);

        var tenantId =
            GetRequiredTenantId();

        if (capabilityOverride.TenantId !=
            tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant commerce capability override deletion was blocked.");
        }

        lock (_store.SyncRoot)
        {
            _store.Items.Remove(
                capabilityOverride);
        }
    }

    private TenantId GetRequiredTenantId()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "An active tenant context is required.");
        }

        return _currentTenant.TenantId.Value;
    }
}