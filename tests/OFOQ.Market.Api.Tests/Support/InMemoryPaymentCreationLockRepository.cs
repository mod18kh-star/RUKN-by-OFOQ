using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryPaymentCreationLockRepository :
    IPaymentCreationLockRepository
{
    private readonly InMemoryOrderStore _orderStore;
    private readonly ICurrentTenant _currentTenant;

    public InMemoryPaymentCreationLockRepository(
        InMemoryOrderStore orderStore,
        ICurrentTenant currentTenant)
    {
        _orderStore = orderStore;
        _currentTenant = currentTenant;
    }

    public Task<Order?> GetOrderForUpdateAsync(
        OrderId orderId,
        UserId customerUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        var tenantId = GetRequiredTenantId();

        lock (_orderStore.SyncRoot)
        {
            var order = _orderStore.Items
                .Select(item => item.Order)
                .SingleOrDefault(item =>
                    item.TenantId == tenantId &&
                    item.Id == orderId);

            return Task.FromResult(order);
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
