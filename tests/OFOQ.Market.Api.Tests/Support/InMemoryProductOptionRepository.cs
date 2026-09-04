using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryProductOptionStore
{
    public object SyncRoot { get; } =
        new();

    public List<ProductOption> Items { get; } =
        [];
}

internal sealed class InMemoryProductOptionRepository :
    IProductOptionRepository
{
    private readonly InMemoryProductOptionStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryProductOptionRepository(
        InMemoryProductOptionStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<ProductOption?> GetByIdAsync(
        ProductOptionId optionId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var option =
                _store.Items
                    .FirstOrDefault(
                        item =>
                            item.Id == optionId &&
                            item.TenantId == tenantId &&
                            !item.IsDeleted);

            return Task.FromResult(
                option);
        }
    }

    public Task<IReadOnlyList<ProductOption>>
        GetByProductIdAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IReadOnlyList<ProductOption> options =
                _store.Items
                    .Where(
                        item =>
                            item.TenantId == tenantId &&
                            item.ProductId == productId &&
                            !item.IsDeleted)
                    .OrderBy(
                        item =>
                            item.SortOrder)
                    .ThenBy(
                        item =>
                            item.Name)
                    .ToArray();

            return Task.FromResult(
                options);
        }
    }

    public Task<bool> NameExistsAsync(
        ProductId productId,
        string normalizedName,
        ProductOptionId? excludingOptionId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            normalizedName);

        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var exists =
                _store.Items.Any(
                    item =>
                        item.TenantId == tenantId &&
                        item.ProductId == productId &&
                        item.NormalizedName == normalizedName &&
                        !item.IsDeleted &&
                        (!excludingOptionId.HasValue ||
                         item.Id != excludingOptionId.Value));

            return Task.FromResult(
                exists);
        }
    }

    public Task AddAsync(
        ProductOption option,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            option);

        var tenantId =
            GetRequiredTenantId();

        if (option.TenantId != tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant product option write was blocked.");
        }

        lock (_store.SyncRoot)
        {
            _store.Items.Add(
                option);
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