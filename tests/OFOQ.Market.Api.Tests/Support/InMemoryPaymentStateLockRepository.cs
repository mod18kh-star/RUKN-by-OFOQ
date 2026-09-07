using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryPaymentStateLockRepository :
    IPaymentStateLockRepository
{
    private readonly InMemoryOrderStore _orderStore;
    private readonly InMemoryPaymentStore _paymentStore;
    private readonly InMemoryPaymentIntentStore _intentStore;
    private readonly ICurrentTenant _currentTenant;

    public InMemoryPaymentStateLockRepository(
        InMemoryOrderStore orderStore,
        InMemoryPaymentStore paymentStore,
        InMemoryPaymentIntentStore intentStore,
        ICurrentTenant currentTenant)
    {
        _orderStore = orderStore;
        _paymentStore = paymentStore;
        _intentStore = intentStore;
        _currentTenant = currentTenant;
    }

    public Task<Order?> GetOrderForUpdateAsync(
        OrderId orderId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        lock (_orderStore.SyncRoot)
        {
            return Task.FromResult(
                _orderStore.Items
                    .Select(item => item.Order)
                    .SingleOrDefault(item =>
                        item.TenantId == tenantId &&
                        item.Id == orderId));
        }
    }

    public Task<Payment?> GetPaymentForUpdateAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        lock (_paymentStore.SyncRoot)
        {
            return Task.FromResult(
                _paymentStore.Items
                    .SingleOrDefault(item =>
                        item.TenantId == tenantId &&
                        item.Id == paymentId));
        }
    }

    public Task<PaymentIntent?> GetIntentForUpdateAsync(
        PaymentIntentId paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        lock (_intentStore.SyncRoot)
        {
            return Task.FromResult(
                _intentStore.Items
                    .Select(item => item.Intent)
                    .SingleOrDefault(item =>
                        item.TenantId == tenantId &&
                        item.Id == paymentIntentId));
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
