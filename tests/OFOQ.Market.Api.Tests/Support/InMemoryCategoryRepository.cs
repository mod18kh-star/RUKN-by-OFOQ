using System.Text;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryCategoryStore
{
    public object SyncRoot { get; } =
        new();

    public List<Category> Items { get; } =
        [];
}

internal sealed class InMemoryCategoryRepository :
    ICategoryRepository
{
    private readonly InMemoryCategoryStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryCategoryRepository(
        InMemoryCategoryStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<Category?> GetByIdAsync(
        CategoryId categoryId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var category =
                _store.Items
                    .FirstOrDefault(
                        item =>
                            item.Id == categoryId &&
                            item.TenantId == tenantId &&
                            !item.IsDeleted);

            return Task.FromResult(
                category);
        }
    }

    public Task<Category?> GetBySlugAsync(
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
            var category =
                _store.Items
                    .FirstOrDefault(
                        item =>
                            item.TenantId == tenantId &&
                            item.Slug == normalizedSlug &&
                            !item.IsDeleted);

            return Task.FromResult(
                category);
        }
    }

    public Task<IReadOnlyList<Category>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IReadOnlyList<Category> categories =
                _store.Items
                    .Where(
                        item =>
                            item.TenantId == tenantId &&
                            !item.IsDeleted)
                    .OrderBy(
                        item =>
                            item.SortOrder)
                    .ThenBy(
                        item =>
                            item.Name)
                    .ToArray();

            return Task.FromResult(
                categories);
        }
    }

    public Task<bool> SlugExistsAsync(
        string slug,
        CategoryId? excludingCategoryId = null,
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
                        (!excludingCategoryId.HasValue ||
                         item.Id != excludingCategoryId.Value));

            return Task.FromResult(
                exists);
        }
    }

    public Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            category);

        var tenantId =
            GetRequiredTenantId();

        if (category.TenantId != tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant category write was blocked.");
        }

        lock (_store.SyncRoot)
        {
            _store.Items.Add(
                category);
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