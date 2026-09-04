using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryProductOptionValueStore
{
    public object SyncRoot { get; } =
        new();

    public List<ProductOptionValue> Items { get; } =
        [];
}

internal sealed class InMemoryProductOptionValueRepository :
    IProductOptionValueRepository
{
    private readonly InMemoryProductOptionValueStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryProductOptionValueRepository(
        InMemoryProductOptionValueStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<ProductOptionValue?> GetByIdAsync(
        ProductOptionValueId valueId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var value =
                _store.Items
                    .FirstOrDefault(
                        item =>
                            item.Id == valueId &&
                            item.TenantId == tenantId &&
                            !item.IsDeleted);

            return Task.FromResult(
                value);
        }
    }

    public Task<IReadOnlyList<ProductOptionValue>>
        GetByOptionIdAsync(
            ProductOptionId optionId,
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IReadOnlyList<ProductOptionValue> values =
                _store.Items
                    .Where(
                        item =>
                            item.TenantId == tenantId &&
                            item.ProductOptionId == optionId &&
                            !item.IsDeleted)
                    .OrderBy(
                        item =>
                            item.SortOrder)
                    .ThenBy(
                        item =>
                            item.Value)
                    .ToArray();

            return Task.FromResult(
                values);
        }
    }

    public Task<bool> ValueExistsAsync(
        ProductOptionId optionId,
        string normalizedValue,
        ProductOptionValueId? excludingValueId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            normalizedValue);

        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var exists =
                _store.Items.Any(
                    item =>
                        item.TenantId == tenantId &&
                        item.ProductOptionId == optionId &&
                        item.NormalizedValue == normalizedValue &&
                        !item.IsDeleted &&
                        (!excludingValueId.HasValue ||
                         item.Id != excludingValueId.Value));

            return Task.FromResult(
                exists);
        }
    }

    public Task AddAsync(
        ProductOptionValue value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        var tenantId =
            GetRequiredTenantId();

        if (value.TenantId != tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant product option value write was blocked.");
        }

        lock (_store.SyncRoot)
        {
            _store.Items.Add(
                value);
        }

        return Task.CompletedTask;
    }

    private OFOQ.Market.Domain.Tenancy.TenantId
        GetRequiredTenantId()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required.");
        }

        return _currentTenant.TenantId.Value;
    }
}