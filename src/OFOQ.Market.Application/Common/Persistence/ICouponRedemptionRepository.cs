using OFOQ.Market.Domain.Commerce.Discounts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
namespace OFOQ.Market.Application.Common.Persistence;
public interface ICouponRedemptionRepository
{
    Task<int> CountForCouponAsync(DiscountCouponId couponId, CancellationToken cancellationToken = default);
    Task<int> CountForCouponAndCustomerAsync(DiscountCouponId couponId, UserId customerUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForOrderAsync(OrderId orderId, CancellationToken cancellationToken = default);
    Task AddAsync(CouponRedemption redemption, CancellationToken cancellationToken = default);
}
