using OFOQ.Market.Domain.Commerce.Returns;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IReturnRequestLockRepository
{
    Task<ReturnRequest?> GetForUpdateAsync(ReturnRequestId id, CancellationToken cancellationToken = default);
}
