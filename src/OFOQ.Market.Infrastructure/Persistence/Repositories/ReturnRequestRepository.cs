using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Returns;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class ReturnRequestRepository : IReturnRequestRepository
{
    private readonly MarketDbContext _db;
    public ReturnRequestRepository(MarketDbContext db) => _db=db;

    public Task<ReturnRequest?> GetByIdAsync(ReturnRequestId id,CancellationToken cancellationToken=default)
        => _db.ReturnRequests.Include("_items").SingleOrDefaultAsync(x=>x.Id==id,cancellationToken);

    public async Task<IReadOnlyList<ReturnRequest>> GetForCustomerAsync(UserId customerUserId,CancellationToken cancellationToken=default)
        => await _db.ReturnRequests.Include("_items").Where(x=>x.CustomerUserId==customerUserId).OrderByDescending(x=>x.CreatedAtUtc).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<ReturnRequest>> GetAllAsync(int take,CancellationToken cancellationToken=default)
        => await _db.ReturnRequests.Include("_items").OrderByDescending(x=>x.CreatedAtUtc).Take(take).ToArrayAsync(cancellationToken);

    public async Task<int> GetReservedQuantityAsync(OrderItemId orderItemId,CancellationToken cancellationToken=default)
    {
        var value = await _db.ReturnRequestItems
            .Where(i => i.OrderItemId == orderItemId && _db.ReturnRequests.Any(r => r.Id == i.ReturnRequestId && r.Status != ReturnRequestStatus.Rejected && r.Status != ReturnRequestStatus.Cancelled))
            .SumAsync(i => (int?)i.Quantity, cancellationToken);
        return value ?? 0;
    }

    public Task AddAsync(ReturnRequest request,CancellationToken cancellationToken=default)
        => _db.ReturnRequests.AddAsync(request,cancellationToken).AsTask();
}
