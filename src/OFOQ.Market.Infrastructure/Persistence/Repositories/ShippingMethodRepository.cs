using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Fulfillment;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class ShippingMethodRepository : IShippingMethodRepository
{
    private readonly MarketDbContext _db;
    public ShippingMethodRepository(MarketDbContext db) => _db = db;
    public async Task<IReadOnlyList<ShippingMethod>> GetAllAsync(CancellationToken cancellationToken = default) => await _db.ShippingMethods.ToListAsync(cancellationToken);
    public Task<ShippingMethod?> GetByIdAsync(ShippingMethodId id, CancellationToken cancellationToken = default) => _db.ShippingMethods.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<bool> CodeExistsAsync(string normalizedCode, ShippingMethodId? excludingId = null, CancellationToken cancellationToken = default)
        => _db.ShippingMethods.AnyAsync(x => x.Code == normalizedCode && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
    public Task AddAsync(ShippingMethod method, CancellationToken cancellationToken = default) => _db.ShippingMethods.AddAsync(method, cancellationToken).AsTask();
}
