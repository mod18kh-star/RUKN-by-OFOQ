using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
namespace OFOQ.Market.Domain.Commerce.Discounts;

public sealed class CouponRedemption : Entity<CouponRedemptionId>, ITenantDataScoped
{
    private CouponRedemption() { }
    private CouponRedemption(CouponRedemptionId id, TenantId tenantId, DiscountCouponId couponId, OrderId orderId, UserId customerUserId, decimal discountAmount, DateTimeOffset redeemedAtUtc) : base(id)
    {
        if (tenantId.IsEmpty || couponId.IsEmpty || orderId.IsEmpty || customerUserId.IsEmpty) throw new ArgumentException("Coupon redemption identifiers are required.");
        if (discountAmount <= 0m) throw new ArgumentOutOfRangeException(nameof(discountAmount));
        TenantId = tenantId; CouponId = couponId; OrderId = orderId; CustomerUserId = customerUserId; DiscountAmount = decimal.Round(discountAmount,2,MidpointRounding.AwayFromZero); RedeemedAtUtc = redeemedAtUtc;
    }
    public TenantId TenantId { get; private set; }
    public DiscountCouponId CouponId { get; private set; }
    public OrderId OrderId { get; private set; }
    public UserId CustomerUserId { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public DateTimeOffset RedeemedAtUtc { get; private set; }
    public static CouponRedemption Create(TenantId tenantId, DiscountCouponId couponId, OrderId orderId, UserId customerUserId, decimal discountAmount, DateTimeOffset at) => new(CouponRedemptionId.New(),tenantId,couponId,orderId,customerUserId,discountAmount,at);
}
