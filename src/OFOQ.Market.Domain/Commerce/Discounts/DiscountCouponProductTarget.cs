using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;
namespace OFOQ.Market.Domain.Commerce.Discounts;

public sealed class DiscountCouponProductTarget : Entity<DiscountCouponTargetId>, ITenantDataScoped
{
    private DiscountCouponProductTarget() { }
    private DiscountCouponProductTarget(DiscountCouponTargetId id, TenantId tenantId, DiscountCouponId couponId, ProductId productId) : base(id)
    {
        if (tenantId.IsEmpty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (couponId.IsEmpty) throw new ArgumentException("Coupon ID cannot be empty.", nameof(couponId));
        if (productId.IsEmpty) throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
        TenantId = tenantId; CouponId = couponId; ProductId = productId;
    }
    public TenantId TenantId { get; private set; }
    public DiscountCouponId CouponId { get; private set; }
    public ProductId ProductId { get; private set; }
    public static DiscountCouponProductTarget Create(TenantId tenantId, DiscountCouponId couponId, ProductId productId) => new(DiscountCouponTargetId.New(), tenantId, couponId, productId);
}
