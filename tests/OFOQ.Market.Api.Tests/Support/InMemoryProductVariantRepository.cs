using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryProductVariantStore
{
    public object SyncRoot { get; } =
        new();

    public List<ProductVariant> Items { get; } =
        [];
}

internal sealed class InMemoryProductVariantRepository :
    IProductVariantRepository
{
    private readonly InMemoryProductVariantStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryProductVariantRepository(
        InMemoryProductVariantStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<ProductVariant?> GetByIdAsync(
        ProductVariantId variantId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var variant =
                _store.Items
                    .FirstOrDefault(
                        item =>
                            item.Id == variantId &&
                            item.TenantId == tenantId &&
                            !item.IsDeleted);

            return Task.FromResult(
                variant);
        }
    }

    public Task<ProductVariant?> GetBySkuAsync(
        ProductSku sku,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var variant =
                _store.Items
                    .FirstOrDefault(
                        item =>
                            item.TenantId == tenantId &&
                            item.Sku == sku &&
                            !item.IsDeleted);

            return Task.FromResult(
                variant);
        }
    }

    public Task<IReadOnlyList<ProductVariant>>
        GetByProductIdAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IReadOnlyList<ProductVariant> variants =
                _store.Items
                    .Where(
                        item =>
                            item.TenantId == tenantId &&
                            item.ProductId == productId &&
                            !item.IsDeleted)
                    .OrderByDescending(
                        item =>
                            item.IsDefault)
                    .ThenBy(
                        item =>
                            item.Name)
                    .ToArray();

            return Task.FromResult(
                variants);
        }
    }

    public Task<bool> SkuExistsAsync(
        ProductSku sku,
        ProductVariantId? excludingVariantId = null,
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
                        item.Sku == sku &&
                        !item.IsDeleted &&
                        (!excludingVariantId.HasValue ||
                         item.Id != excludingVariantId.Value));

            return Task.FromResult(
                exists);
        }
    }

    public Task AddAsync(
        ProductVariant variant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            variant);

        var tenantId =
            GetRequiredTenantId();

        if (variant.TenantId !=
            tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant product variant write was blocked.");
        }

        lock (_store.SyncRoot)
        {
            _store.Items.Add(
                variant);
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

        return _currentTenant
            .TenantId
            .Value;
    }
}