using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryTenantPaymentMethodStore
{
    public object SyncRoot { get; } = new();
    public List<TenantPaymentMethod> Items { get; } = [];
}

internal sealed class InMemoryTenantPaymentMethodRepository :
    ITenantPaymentMethodRepository
{
    private readonly InMemoryTenantPaymentMethodStore _store;
    private readonly ICurrentTenant _currentTenant;

    public InMemoryTenantPaymentMethodRepository(
        InMemoryTenantPaymentMethodStore store,
        ICurrentTenant currentTenant)
    {
        _store = store;
        _currentTenant = currentTenant;
    }

    public Task<TenantPaymentMethod?> GetByIdAsync(
        TenantPaymentMethodId paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            return Task.FromResult(
                _store.Items.SingleOrDefault(item =>
                    item.TenantId == tenantId &&
                    item.Id == paymentMethodId));
        }
    }

    public Task<IReadOnlyList<TenantPaymentMethod>> GetEnabledAsync(
        CurrencyCode currency,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IReadOnlyList<TenantPaymentMethod> result = _store.Items
                .Where(item =>
                    item.TenantId == tenantId &&
                    item.IsEnabled &&
                    item.Currency == currency)
                .OrderBy(item => item.DisplayName)
                .ToArray();

            return Task.FromResult(result);
        }
    }

    public Task AddAsync(
        TenantPaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentMethod);
        var tenantId = GetRequiredTenantId();

        if (paymentMethod.TenantId != tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant payment method creation was blocked.");
        }

        lock (_store.SyncRoot)
        {
            _store.Items.Add(paymentMethod);
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
