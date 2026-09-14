using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class MerchantOrderQueryRepository :
    IMerchantOrderQueryRepository
{
    private readonly MarketDbContext
        _dbContext;

    public MerchantOrderQueryRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<Order>> GetAsync(
        OrderStatus? status,
        OrderFulfillmentStatus? fulfillmentStatus,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (take <= 0 ||
            take > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take),
                "Order query take must be between 1 and 100.");
        }

        IQueryable<Order> query =
            _dbContext
                .Orders
                .AsNoTracking()
                .Include("_items");

        if (status.HasValue)
        {
            query =
                query.Where(
                    order =>
                        order.Status ==
                        status.Value);
        }

        if (fulfillmentStatus.HasValue)
        {
            query =
                query.Where(
                    order =>
                        order.FulfillmentStatus ==
                        fulfillmentStatus.Value);
        }

        return await query
            .OrderByDescending(
                order =>
                    order.CreatedAtUtc)
            .Take(
                take)
            .ToListAsync(
                cancellationToken);
    }
}