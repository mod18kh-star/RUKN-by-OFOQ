using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Catalog.Attributes;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryProductAttributeValueStore
{
    public object SyncRoot { get; } =
        new();

    public List<ProductAttributeValue> Items { get; } =
        [];
}

internal sealed class InMemoryProductAttributeValueRepository :
    IProductAttributeValueRepository
{
    private readonly InMemoryProductAttributeValueStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryProductAttributeValueRepository(
        InMemoryProductAttributeValueStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<IReadOnlyList<ProductAttributeValue>>
        GetByProductIdAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IReadOnlyList<ProductAttributeValue> result =
                _store.Items
                    .Where(
                        item =>
                            item.TenantId ==
                                tenantId &&
                            item.ProductId ==
                                productId)
                    .OrderBy(
                        item =>
                            item.Key)
                    .ToArray();

            return Task.FromResult(
                result);
        }
    }

    public Task AddAsync(
        ProductAttributeValue value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        var tenantId =
            GetRequiredTenantId();

        if (value.TenantId !=
            tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant product attribute creation was blocked.");
        }

        lock (_store.SyncRoot)
        {
            var duplicate =
                _store.Items.Any(
                    item =>
                        item.TenantId ==
                            tenantId &&
                        item.ProductId ==
                            value.ProductId &&
                        item.Key ==
                            value.Key);

            if (duplicate)
            {
                throw new InvalidOperationException(
                    "A product attribute with this key already exists.");
            }

            _store.Items.Add(
                value);
        }

        return Task.CompletedTask;
    }

    public void Remove(
        ProductAttributeValue value)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        var tenantId =
            GetRequiredTenantId();

        if (value.TenantId !=
            tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant product attribute deletion was blocked.");
        }

        lock (_store.SyncRoot)
        {
            _store.Items.Remove(
                value);
        }
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