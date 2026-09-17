using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;
namespace OFOQ.Market.Domain.Commerce.Discounts;

public sealed class DiscountCouponCategoryTarget : Entity<DiscountCouponTargetId>, ITenantDataScoped
{
    private DiscountCouponCategoryTarget() { }
    private DiscountCouponCategoryTarget(DiscountCouponTargetId id, TenantId tenantId, DiscountCouponId couponId, CategoryId categoryId) : base(id)
    {
        if (tenantId.IsEmpty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (couponId.IsEmpty) throw new ArgumentException("Coupon ID cannot be empty.", nameof(couponId));
        if (categoryId.IsEmpty) throw new ArgumentException("Category ID cannot be empty.", nameof(categoryId));
        TenantId = tenantId; CouponId = couponId; CategoryId = categoryId;
    }
    public TenantId TenantId { get; private set; }
    public DiscountCouponId CouponId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public static DiscountCouponCategoryTarget Create(TenantId tenantId, DiscountCouponId couponId, CategoryId categoryId) => new(DiscountCouponTargetId.New(), tenantId, couponId, categoryId);
}
