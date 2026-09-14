using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IOrderStateLockRepository
{
    Task<Order?> GetOrderForUpdateAsync(
        OrderId orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductVariant>>
        GetVariantsForUpdateAsync(
            IReadOnlyCollection<ProductVariantId> variantIds,
            CancellationToken cancellationToken = default);
}