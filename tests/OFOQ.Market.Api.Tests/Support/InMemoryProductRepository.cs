using System.Text;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryProductStore
{
    public object SyncRoot { get; } =
        new();

    public List<Product> Items { get; } =
        [];
}

internal sealed class InMemoryProductRepository :
    IProductRepository
{
    private readonly InMemoryProductStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryProductRepository(
        InMemoryProductStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<Product?> GetByIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var product =
                _store.Items
                    .FirstOrDefault(
                        item =>
                            item.Id == productId &&
                            item.TenantId == tenantId &&
                            !item.IsDeleted);

            return Task.FromResult(
                product);
        }
    }

    public Task<Product?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            slug);

        var tenantId =
            GetRequiredTenantId();

        var normalizedSlug =
            NormalizeSlug(
                slug);

        lock (_store.SyncRoot)
        {
            var product =
                _store.Items
                    .FirstOrDefault(
                        item =>
                            item.TenantId == tenantId &&
                            item.Slug == normalizedSlug &&
                            !item.IsDeleted);

            return Task.FromResult(
                product);
        }
    }

    public Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IReadOnlyList<Product> products =
                _store.Items
                    .Where(
                        item =>
                            item.TenantId == tenantId &&
                            !item.IsDeleted)
                    .OrderByDescending(
                        item =>
                            item.CreatedAtUtc)
                    .ToArray();

            return Task.FromResult(
                products);
        }
    }

    public Task<bool> SlugExistsAsync(
        string slug,
        ProductId? excludingProductId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            slug);

        var tenantId =
            GetRequiredTenantId();

        var normalizedSlug =
            NormalizeSlug(
                slug);

        lock (_store.SyncRoot)
        {
            var exists =
                _store.Items.Any(
                    item =>
                        item.TenantId == tenantId &&
                        item.Slug == normalizedSlug &&
                        !item.IsDeleted &&
                        (!excludingProductId.HasValue ||
                         item.Id != excludingProductId.Value));

            return Task.FromResult(
                exists);
        }
    }

    public Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            product);

        var tenantId =
            GetRequiredTenantId();

        if (product.TenantId !=
            tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant product write was blocked.");
        }

        lock (_store.SyncRoot)
        {
            _store.Items.Add(
                product);
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

    private static string NormalizeSlug(
        string slug)
    {
        return slug
            .Trim()
            .Normalize(
                NormalizationForm.FormKC)
            .ToLowerInvariant();
    }
}