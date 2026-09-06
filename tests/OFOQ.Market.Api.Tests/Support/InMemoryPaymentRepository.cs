using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryPaymentStore
{
    public object SyncRoot { get; } = new();
    public List<Payment> Items { get; } = [];
}

internal sealed class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly InMemoryPaymentStore _store;
    private readonly ICurrentTenant _currentTenant;

    public InMemoryPaymentRepository(
        InMemoryPaymentStore store,
        ICurrentTenant currentTenant)
    {
        _store = store;
        _currentTenant = currentTenant;
    }

    public Task<Payment?> GetByIdAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            return Task.FromResult(
                _store.Items.SingleOrDefault(item =>
                    item.TenantId == tenantId &&
                    item.Id == paymentId));
        }
    }

    public Task<Payment?> GetByOrderIdAsync(
        OrderId orderId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        lock (_store.SyncRoot)
        {
            return Task.FromResult(
                _store.Items.SingleOrDefault(item =>
                    item.TenantId == tenantId &&
                    item.OrderId == orderId));
        }
    }

    public Task AddAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);
        var tenantId = GetRequiredTenantId();

        if (payment.TenantId != tenantId)
        {
            throw new TenantScopeViolationException(
                "Cross-tenant payment creation was blocked.");
        }

        lock (_store.SyncRoot)
        {
            if (_store.Items.Any(item =>
                    item.TenantId == tenantId &&
                    item.OrderId == payment.OrderId))
            {
                throw new InvalidOperationException(
                    "The order already has a payment.");
            }

            _store.Items.Add(payment);
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
