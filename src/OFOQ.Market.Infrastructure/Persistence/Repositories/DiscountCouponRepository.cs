using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Discounts;
namespace OFOQ.Market.Infrastructure.Persistence.Repositories;
public sealed class DiscountCouponRepository : IDiscountCouponRepository
{
    private readonly MarketDbContext _db; public DiscountCouponRepository(MarketDbContext db) => _db = db;
    private IQueryable<DiscountCoupon> Query => _db.DiscountCoupons.Include("_productTargets").Include("_categoryTargets");
    public async Task<IReadOnlyList<DiscountCoupon>> GetAllAsync(CancellationToken cancellationToken = default) => await Query.ToListAsync(cancellationToken);
    public Task<DiscountCoupon?> GetByIdAsync(DiscountCouponId id, CancellationToken cancellationToken = default) => Query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<DiscountCoupon?> GetByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default) => Query.SingleOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);
    public Task<bool> CodeExistsAsync(string normalizedCode, DiscountCouponId? excludingId = null, CancellationToken cancellationToken = default) => _db.DiscountCoupons.AnyAsync(x => x.Code == normalizedCode && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
    public Task AddAsync(DiscountCoupon coupon, CancellationToken cancellationToken = default) => _db.DiscountCoupons.AddAsync(coupon, cancellationToken).AsTask();
}
