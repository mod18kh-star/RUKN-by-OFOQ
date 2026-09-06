using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class PaymentCreationLockRepository :
    IPaymentCreationLockRepository
{
    private readonly MarketDbContext _dbContext;

    public PaymentCreationLockRepository(MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order?> GetOrderForUpdateAsync(
        OrderId orderId,
        UserId customerUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        if (orderId.IsEmpty)
        {
            throw new ArgumentException(
                "Order ID cannot be empty.",
                nameof(orderId));
        }

        if (customerUserId.IsEmpty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(customerUserId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        var lockKey = string.Concat(
            tenantId.Value.ToString("N"),
            ":",
            customerUserId.Value.ToString("N"),
            ":",
            idempotencyKey.Trim());

        /*
         * The advisory transaction lock serializes reuse of the
         * same Idempotency-Key even when two requests target
         * different Orders. A hash collision only over-serializes;
         * correctness is still protected by database constraints.
         *
         * This method must run inside ITransactionExecutor.
         */
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);

        /*
         * Lock the Order row as the stable aggregate boundary for
         * payment creation. This prevents concurrent requests with
         * different keys from racing to create the one Payment row
         * allowed for an Order.
         */
        var order = await _dbContext.Orders
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM commerce_orders
                WHERE
                    tenant_id = {tenantId.Value}
                    AND id = {orderId.Value}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            return null;
        }

        await _dbContext.Entry(order)
            .Collection("_items")
            .LoadAsync(cancellationToken);

        return order;
    }

    private OFOQ.Market.Domain.Tenancy.TenantId GetRequiredTenantId()
    {
        if (!_dbContext.HasCurrentTenant ||
            _dbContext.CurrentTenantId.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required for payment creation locking.");
        }

        return _dbContext.CurrentTenantId;
    }
}
