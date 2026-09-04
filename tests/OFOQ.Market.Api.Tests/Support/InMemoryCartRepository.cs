using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryCartStore
{
    public object SyncRoot { get; } =
        new();

    public List<Cart> Items { get; } =
        [];
}

internal sealed class InMemoryCartRepository :
    ICartRepository
{
    private readonly InMemoryCartStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryCartRepository(
        InMemoryCartStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<Cart?> GetByIdAsync(
        CartId cartId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var cart =
                _store.Items
                    .SingleOrDefault(
                        item =>
                            item.TenantId ==
                            tenantId &&
                            item.Id ==
                            cartId);

            return Task.FromResult(
                cart);
        }
    }

    public Task<Cart?> GetActiveByCustomerUserIdAsync(
        UserId customerUserId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var cart =
                _store.Items
                    .SingleOrDefault(
                        item =>
                            item.TenantId ==
                            tenantId &&
                            item.CustomerUserId ==
                            customerUserId &&
                            item.Status ==
                            CartStatus.Active);

            return Task.FromResult(
                cart);
        }
    }

    public Task AddAsync(
        Cart cart,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            cart);

        var tenantId =
            GetRequiredTenantId();

        if (cart.TenantId !=
            tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant cart creation was blocked.");
        }

        lock (_store.SyncRoot)
        {
            _store.Items.Add(
                cart);
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
                "A tenant context is required.");
        }

        return _currentTenant.TenantId.Value;
    }
}