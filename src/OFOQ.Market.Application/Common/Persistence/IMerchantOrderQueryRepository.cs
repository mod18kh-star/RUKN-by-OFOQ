using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IMerchantOrderQueryRepository
{
    Task<IReadOnlyList<Order>> GetAsync(
        OrderStatus? status,
        OrderFulfillmentStatus? fulfillmentStatus,
        int take,
        CancellationToken cancellationToken = default);
}