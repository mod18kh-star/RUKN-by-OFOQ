using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryTenantCommerceVerticalStore
{
    public object SyncRoot { get; } =
        new();

    public List<TenantCommerceVertical> Items { get; } =
        [];
}

internal sealed class InMemoryTenantCommerceVerticalRepository :
    ITenantCommerceVerticalRepository
{
    private readonly InMemoryTenantCommerceVerticalStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryTenantCommerceVerticalRepository(
        InMemoryTenantCommerceVerticalStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<IReadOnlyList<TenantCommerceVertical>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IReadOnlyList<TenantCommerceVertical> result =
                _store.Items
                    .Where(
                        item =>
                            item.TenantId ==
                            tenantId)
                    .OrderByDescending(
                        item =>
                            item.IsPrimary)
                    .ThenBy(
                        item =>
                            item.VerticalType)
                    .ToArray();

            return Task.FromResult(
                result);
        }
    }

    public Task<TenantCommerceVertical?> GetByTypeAsync(
        CommerceVerticalType verticalType,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var vertical =
                _store.Items.SingleOrDefault(
                    item =>
                        item.TenantId ==
                            tenantId &&
                        item.VerticalType ==
                            verticalType);

            return Task.FromResult(
                vertical);
        }
    }

    public Task AddAsync(
        TenantCommerceVertical vertical,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            vertical);

        var tenantId =
            GetRequiredTenantId();

        if (vertical.TenantId !=
            tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant commerce vertical creation was blocked.");
        }

        lock (_store.SyncRoot)
        {
            var duplicate =
                _store.Items.Any(
                    item =>
                        item.TenantId ==
                            tenantId &&
                        item.VerticalType ==
                            vertical.VerticalType);

            if (duplicate)
            {
                throw new InvalidOperationException(
                    "A commerce vertical of this type already exists for the tenant.");
            }

            _store.Items.Add(
                vertical);
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
                "An active tenant context is required.");
        }

        return _currentTenant.TenantId.Value;
    }
}