using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Discounts;
using OFOQ.Market.Domain.Tenancy;
namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;
public sealed class DiscountCouponProductTargetConfiguration : IEntityTypeConfiguration<DiscountCouponProductTarget>
{
    public void Configure(EntityTypeBuilder<DiscountCouponProductTarget> b)
    {
        b.ToTable("commerce_discount_coupon_products"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasConversion(x => x.Value, x => DiscountCouponTargetId.From(x)).ValueGeneratedNever(); b.Property(x => x.TenantId).HasColumnName("tenant_id").HasConversion(x => x.Value, x => TenantId.From(x)).IsRequired(); b.Property(x => x.CouponId).HasColumnName("coupon_id").HasConversion(x => x.Value, x => DiscountCouponId.From(x)).IsRequired(); b.Property(x => x.ProductId).HasColumnName("product_id").HasConversion(x => x.Value, x => ProductId.From(x)).IsRequired();
        b.HasIndex(x => new { x.TenantId, x.CouponId, x.ProductId }).IsUnique();
    }
}
