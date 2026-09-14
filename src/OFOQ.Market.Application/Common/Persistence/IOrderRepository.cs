using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        OrderId orderId,
        CancellationToken cancellationToken = default);

    Task<Order?> GetBySourceCartIdAsync(
        CartId sourceCartId,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByCheckoutIdempotencyKeyAsync(
        UserId customerUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default);


    Task AddAsync(
        Order order,
        string checkoutIdempotencyKey,
        CancellationToken cancellationToken = default);
}