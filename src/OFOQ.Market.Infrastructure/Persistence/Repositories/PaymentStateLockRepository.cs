using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class PaymentStateLockRepository :
    IPaymentStateLockRepository
{
    private readonly MarketDbContext _dbContext;

    public PaymentStateLockRepository(MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order?> GetOrderForUpdateAsync(
        OrderId orderId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        if (orderId.IsEmpty)
        {
            throw new ArgumentException(
                "Order ID cannot be empty.",
                nameof(orderId));
        }

        var order = await _dbContext.Orders
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM commerce_orders
                WHERE tenant_id = {tenantId.Value}
                  AND id = {orderId.Value}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is not null)
        {
            await _dbContext.Entry(order)
                .Collection("_items")
                .LoadAsync(cancellationToken);
        }

        return order;
    }

    public Task<Payment?> GetPaymentForUpdateAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        if (paymentId.IsEmpty)
        {
            throw new ArgumentException(
                "Payment ID cannot be empty.",
                nameof(paymentId));
        }

        return _dbContext.Payments
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM commerce_payments
                WHERE tenant_id = {tenantId.Value}
                  AND id = {paymentId.Value}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PaymentIntent?> GetIntentForUpdateAsync(
        PaymentIntentId paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        if (paymentIntentId.IsEmpty)
        {
            throw new ArgumentException(
                "Payment intent ID cannot be empty.",
                nameof(paymentIntentId));
        }

        var intent = await _dbContext.PaymentIntents
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM commerce_payment_intents
                WHERE tenant_id = {tenantId.Value}
                  AND id = {paymentIntentId.Value}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (intent is not null)
        {
            await _dbContext.Entry(intent)
                .Collection("_transactions")
                .LoadAsync(cancellationToken);
        }

        return intent;
    }

    private OFOQ.Market.Domain.Tenancy.TenantId GetRequiredTenantId()
    {
        if (!_dbContext.HasCurrentTenant ||
            _dbContext.CurrentTenantId.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required for payment state locking.");
        }

        return _dbContext.CurrentTenantId;
    }
}
