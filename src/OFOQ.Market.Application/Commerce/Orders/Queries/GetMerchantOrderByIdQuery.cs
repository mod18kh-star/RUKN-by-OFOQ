using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Application.Commerce.Orders.Queries;

public sealed record GetMerchantOrderByIdQuery(
    OrderId OrderId);