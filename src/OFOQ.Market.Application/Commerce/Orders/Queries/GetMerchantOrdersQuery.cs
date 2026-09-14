using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Application.Commerce.Orders.Queries;

public sealed record GetMerchantOrdersQuery(
    OrderStatus? Status = null,
    OrderFulfillmentStatus? FulfillmentStatus = null,
    int Take = 50);