using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Fulfillment;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class FulfillmentLocationRepository : IFulfillmentLocationRepository
{
    private readonly MarketDbContext _db;
    public FulfillmentLocationRepository(MarketDbContext db) => _db = db;
    public async Task<IReadOnlyList<FulfillmentLocation>> GetAllAsync(CancellationToken cancellationToken = default) => await _db.FulfillmentLocations.ToListAsync(cancellationToken);
    public Task<FulfillmentLocation?> GetByIdAsync(FulfillmentLocationId id, CancellationToken cancellationToken = default) => _db.FulfillmentLocations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<bool> CodeExistsAsync(string normalizedCode, FulfillmentLocationId? excludingId = null, CancellationToken cancellationToken = default)
        => _db.FulfillmentLocations.AnyAsync(x => x.Code == normalizedCode && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
    public Task AddAsync(FulfillmentLocation location, CancellationToken cancellationToken = default) => _db.FulfillmentLocations.AddAsync(location, cancellationToken).AsTask();
}
