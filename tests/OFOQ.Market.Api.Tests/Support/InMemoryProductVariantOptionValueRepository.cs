using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryProductVariantOptionValueStore
{
    public object SyncRoot { get; } =
        new();

    public List<ProductVariantOptionValue> Items { get; } =
        [];
}

internal sealed class InMemoryProductVariantOptionValueRepository :
    IProductVariantOptionValueRepository
{
    private readonly InMemoryProductVariantOptionValueStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryProductVariantOptionValueRepository(
        InMemoryProductVariantOptionValueStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<IReadOnlyList<ProductVariantOptionValue>>
        GetByVariantIdAsync(
            ProductVariantId variantId,
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IReadOnlyList<ProductVariantOptionValue> assignments =
                _store.Items
                    .Where(
                        item =>
                            item.TenantId == tenantId &&
                            item.ProductVariantId == variantId &&
                            !item.IsDeleted)
                    .ToArray();

            return Task.FromResult(
                assignments);
        }
    }

    public Task<bool> ExistsForOptionAsync(
        ProductVariantId variantId,
        ProductOptionId optionId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var exists =
                _store.Items.Any(
                    item =>
                        item.TenantId == tenantId &&
                        item.ProductVariantId == variantId &&
                        item.ProductOptionId == optionId &&
                        !item.IsDeleted);

            return Task.FromResult(
                exists);
        }
    }

    public Task AddAsync(
        ProductVariantOptionValue assignment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            assignment);

        var tenantId =
            GetRequiredTenantId();

        if (assignment.TenantId != tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant product variant option assignment was blocked.");
        }

        lock (_store.SyncRoot)
        {
            _store.Items.Add(
                assignment);
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