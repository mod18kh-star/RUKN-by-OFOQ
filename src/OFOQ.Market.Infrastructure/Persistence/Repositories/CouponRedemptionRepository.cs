using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Discounts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
namespace OFOQ.Market.Infrastructure.Persistence.Repositories;
public sealed class CouponRedemptionRepository : ICouponRedemptionRepository
{
    private readonly MarketDbContext _db; public CouponRedemptionRepository(MarketDbContext db)=>_db=db;
    public Task<int> CountForCouponAsync(DiscountCouponId couponId,CancellationToken cancellationToken=default)=>_db.CouponRedemptions.CountAsync(x=>x.CouponId==couponId,cancellationToken);
    public Task<int> CountForCouponAndCustomerAsync(DiscountCouponId couponId,UserId customerUserId,CancellationToken cancellationToken=default)=>_db.CouponRedemptions.CountAsync(x=>x.CouponId==couponId&&x.CustomerUserId==customerUserId,cancellationToken);
    public Task<bool> ExistsForOrderAsync(OrderId orderId,CancellationToken cancellationToken=default)=>_db.CouponRedemptions.AnyAsync(x=>x.OrderId==orderId,cancellationToken);
    public Task AddAsync(CouponRedemption redemption,CancellationToken cancellationToken=default)=>_db.CouponRedemptions.AddAsync(redemption,cancellationToken).AsTask();
}
