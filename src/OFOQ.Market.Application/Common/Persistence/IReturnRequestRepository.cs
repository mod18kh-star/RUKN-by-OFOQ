using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Returns;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IReturnRequestRepository
{
    Task<ReturnRequest?> GetByIdAsync(ReturnRequestId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturnRequest>> GetForCustomerAsync(UserId customerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturnRequest>> GetAllAsync(int take, CancellationToken cancellationToken = default);
    Task<int> GetReservedQuantityAsync(OrderItemId orderItemId, CancellationToken cancellationToken = default);
    Task AddAsync(ReturnRequest request, CancellationToken cancellationToken = default);
}
