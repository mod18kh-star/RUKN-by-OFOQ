using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository :
    IOrderRepository
{
    private readonly MarketDbContext
        _dbContext;

    public OrderRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<Order?> GetByIdAsync(
        OrderId orderId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Orders
            .Include("_items")
            .SingleOrDefaultAsync(
                order =>
                    order.Id ==
                    orderId,
                cancellationToken);
    }

    public Task<Order?> GetBySourceCartIdAsync(
        CartId sourceCartId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Orders
            .Include("_items")
            .SingleOrDefaultAsync(
                order =>
                    EF.Property<CartId>(
                        order,
                        "_sourceCartId") ==
                    sourceCartId,
                cancellationToken);
    }


    public Task<Order?> GetByCheckoutIdempotencyKeyAsync(
        UserId customerUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (customerUserId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(customerUserId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            idempotencyKey);

        return _dbContext
            .Orders
            .Include("_items")
            .SingleOrDefaultAsync(
                order =>
                    EF.Property<Guid>(
                        order,
                        "_customerUserId") ==
                    customerUserId.Value &&
                    EF.Property<string?>(
                        order,
                        "CheckoutIdempotencyKey") ==
                    idempotencyKey,
                cancellationToken);
    }

    public Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            order);

        return _dbContext
            .Orders
            .AddAsync(
                order,
                cancellationToken)
            .AsTask();
    }


    public Task AddAsync(
        Order order,
        string checkoutIdempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            order);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            checkoutIdempotencyKey);

        var entry =
            _dbContext.Orders.Add(
                order);

        entry.Property<string?>(
                "CheckoutIdempotencyKey")
            .CurrentValue =
            checkoutIdempotencyKey;

        return Task.CompletedTask;
    }
}
