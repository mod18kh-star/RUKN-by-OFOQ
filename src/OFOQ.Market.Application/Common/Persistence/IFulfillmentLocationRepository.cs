using OFOQ.Market.Domain.Commerce.Fulfillment;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IFulfillmentLocationRepository
{
    Task<IReadOnlyList<FulfillmentLocation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FulfillmentLocation?> GetByIdAsync(FulfillmentLocationId id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string normalizedCode, FulfillmentLocationId? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(FulfillmentLocation location, CancellationToken cancellationToken = default);
}
