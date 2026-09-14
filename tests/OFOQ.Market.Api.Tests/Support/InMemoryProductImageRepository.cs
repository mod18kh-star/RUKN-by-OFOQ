using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryProductImageStore
{
    public object SyncRoot { get; } =
        new();

    public List<ProductImage> Items { get; } =
        [];
}

internal sealed class InMemoryProductImageRepository :
    IProductImageRepository
{
    private readonly InMemoryProductImageStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryProductImageRepository(
        InMemoryProductImageStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<IReadOnlyList<ProductImage>> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IReadOnlyList<ProductImage> result =
                _store.Items
                    .Where(
                        image =>
                            image.TenantId == tenantId &&
                            image.ProductId == productId)
                    .OrderBy(
                        image =>
                            image.SortOrder)
                    .ToArray();

            return Task.FromResult(
                result);
        }
    }

    public Task AddRangeAsync(
        IReadOnlyCollection<ProductImage> images,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            foreach (var image in images)
            {
                if (image.TenantId !=
                    tenantId)
                {
                    throw new TenantScopeViolationException(
                        "Cross-tenant product image creation was blocked.");
                }

                _store.Items.Add(
                    image);
            }
        }

        return Task.CompletedTask;
    }

    public void RemoveRange(
        IReadOnlyCollection<ProductImage> images)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            foreach (var image in images)
            {
                if (image.TenantId !=
                    tenantId)
                {
                    throw new TenantScopeViolationException(
                        "Cross-tenant product image deletion was blocked.");
                }

                _store.Items.Remove(
                    image);
            }
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