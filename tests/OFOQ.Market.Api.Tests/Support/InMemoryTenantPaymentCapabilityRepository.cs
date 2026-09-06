using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryTenantPaymentCapabilityStore
{
    public object SyncRoot { get; } = new();
    public List<TenantPaymentCapability> Items { get; } = [];
}

internal sealed class InMemoryTenantPaymentCapabilityRepository :
    ITenantPaymentCapabilityRepository
{
    private readonly InMemoryTenantPaymentCapabilityStore _store;
    private readonly ICurrentTenant _currentTenant;

    public InMemoryTenantPaymentCapabilityRepository(
        InMemoryTenantPaymentCapabilityStore store,
        ICurrentTenant currentTenant)
    {
        _store = store;
        _currentTenant = currentTenant;
    }

    public Task<TenantPaymentCapability?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            return Task.FromResult(
                _store.Items.SingleOrDefault(item =>
                    item.TenantId == tenantId));
        }
    }

    public Task AddAsync(
        TenantPaymentCapability capability,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capability);
        var tenantId = GetRequiredTenantId();

        if (capability.TenantId != tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant payment capability creation was blocked.");
        }

        lock (_store.SyncRoot)
        {
            _store.Items.RemoveAll(item => item.TenantId == tenantId);
            _store.Items.Add(capability);
        }

        return Task.CompletedTask;
    }

    private TenantId GetRequiredTenantId()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required.");
        }

        return _currentTenant.TenantId.Value;
    }
}
