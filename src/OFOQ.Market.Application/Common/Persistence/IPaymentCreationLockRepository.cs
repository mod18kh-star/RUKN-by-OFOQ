using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IPaymentCreationLockRepository
{
    Task<Order?> GetOrderForUpdateAsync(
        OrderId orderId,
        UserId customerUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
