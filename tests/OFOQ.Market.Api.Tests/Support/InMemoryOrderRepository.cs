using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryOrderStore
{
    public object SyncRoot { get; } =
        new();

    public List<InMemoryOrderEntry> Items { get; } =
        [];
}

internal sealed record InMemoryOrderEntry(
    Order Order,
    string? CheckoutIdempotencyKey);

internal sealed class InMemoryOrderRepository :
    IOrderRepository
{
    private readonly InMemoryOrderStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryOrderRepository(
        InMemoryOrderStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<Order?> GetByIdAsync(
        OrderId orderId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var order =
                _store.Items
                    .Select(
                        item =>
                            item.Order)
                    .SingleOrDefault(
                        item =>
                            item.TenantId ==
                            tenantId &&
                            item.Id ==
                            orderId);

            return Task.FromResult(
                order);
        }
    }

    public Task<Order?> GetBySourceCartIdAsync(
        CartId sourceCartId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var order =
                _store.Items
                    .Select(
                        item =>
                            item.Order)
                    .SingleOrDefault(
                        item =>
                            item.TenantId ==
                            tenantId &&
                            item.SourceCartId ==
                            sourceCartId);

            return Task.FromResult(
                order);
        }
    }

    public Task<Order?> GetByCheckoutIdempotencyKeyAsync(
        UserId customerUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            var order =
                _store.Items
                    .SingleOrDefault(
                        item =>
                            item.Order.TenantId ==
                            tenantId &&
                            item.Order.CustomerUserId ==
                            customerUserId &&
                            string.Equals(
                                item.CheckoutIdempotencyKey,
                                idempotencyKey,
                                StringComparison.Ordinal))
                    ?.Order;

            return Task.FromResult(
                order);
        }
    }

    public Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        return AddCoreAsync(
            order,
            checkoutIdempotencyKey: null);
    }

    public Task AddAsync(
        Order order,
        string checkoutIdempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            checkoutIdempotencyKey);

        return AddCoreAsync(
            order,
            checkoutIdempotencyKey);
    }

    private Task AddCoreAsync(
        Order order,
        string? checkoutIdempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(
            order);

        var tenantId =
            GetRequiredTenantId();

        if (order.TenantId !=
            tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant order creation was blocked.");
        }

        lock (_store.SyncRoot)
        {
            if (_store.Items.Any(
                    item =>
                        item.Order.TenantId ==
                        tenantId &&
                        item.Order.SourceCartId ==
                        order.SourceCartId))
            {
                throw new InvalidOperationException(
                    "The source cart already has an order.");
            }

            if (checkoutIdempotencyKey is not null &&
                _store.Items.Any(
                    item =>
                        item.Order.TenantId ==
                        tenantId &&
                        item.Order.CustomerUserId ==
                        order.CustomerUserId &&
                        string.Equals(
                            item.CheckoutIdempotencyKey,
                            checkoutIdempotencyKey,
                            StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "The checkout idempotency key is already in use.");
            }

            _store.Items.Add(
                new InMemoryOrderEntry(
                    order,
                    checkoutIdempotencyKey));
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
