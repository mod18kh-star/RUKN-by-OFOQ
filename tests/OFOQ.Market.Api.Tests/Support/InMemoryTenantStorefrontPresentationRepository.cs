using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryTenantStorefrontPresentationStore
{
    public object SyncRoot { get; } =
        new();

    public List<TenantStorefrontPresentation> Items { get; } =
        [];
}

internal sealed class InMemoryTenantStorefrontPresentationRepository :
    ITenantStorefrontPresentationRepository
{
    private readonly InMemoryTenantStorefrontPresentationStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryTenantStorefrontPresentationRepository(
        InMemoryTenantStorefrontPresentationStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<TenantStorefrontPresentation?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var presentation =
                _store.Items.SingleOrDefault(
                    item =>
                        item.TenantId ==
                        tenantId);

            return Task.FromResult(
                presentation);
        }
    }

    public Task AddAsync(
        TenantStorefrontPresentation presentation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            presentation);

        var tenantId =
            GetRequiredTenantId();

        if (presentation.TenantId !=
            tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant storefront presentation creation was blocked.");
        }

        lock (_store.SyncRoot)
        {
            if (_store.Items.Any(
                    item =>
                        item.TenantId ==
                        tenantId))
            {
                throw new InvalidOperationException(
                    "Storefront presentation already exists for this tenant.");
            }

            _store.Items.Add(
                presentation);
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
