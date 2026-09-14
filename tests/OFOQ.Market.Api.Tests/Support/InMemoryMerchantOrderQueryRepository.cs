using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryMerchantOrderQueryRepository :
    IMerchantOrderQueryRepository
{
    private readonly InMemoryOrderStore
        _store;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryMerchantOrderQueryRepository(
        InMemoryOrderStore store,
        ICurrentTenant currentTenant)
    {
        _store =
            store;

        _currentTenant =
            currentTenant;
    }

    public Task<IReadOnlyList<Order>> GetAsync(
        OrderStatus? status,
        OrderFulfillmentStatus? fulfillmentStatus,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (take <= 0 ||
            take > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take));
        }

        var tenantId =
            GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            IEnumerable<Order> query =
                _store.Items
                    .Select(
                        item =>
                            item.Order)
                    .Where(
                        order =>
                            order.TenantId ==
                            tenantId);

            if (status.HasValue)
            {
                query =
                    query.Where(
                        order =>
                            order.Status ==
                            status.Value);
            }

            if (fulfillmentStatus.HasValue)
            {
                query =
                    query.Where(
                        order =>
                            order.FulfillmentStatus ==
                            fulfillmentStatus.Value);
            }

            IReadOnlyList<Order> result =
                query
                    .OrderByDescending(
                        order =>
                            order.CreatedAtUtc)
                    .Take(
                        take)
                    .ToArray();

            return Task.FromResult(
                result);
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