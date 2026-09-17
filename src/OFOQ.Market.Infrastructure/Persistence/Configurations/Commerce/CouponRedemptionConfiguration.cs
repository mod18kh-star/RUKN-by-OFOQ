using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Discounts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;
public sealed class CouponRedemptionConfiguration:IEntityTypeConfiguration<CouponRedemption>
{
 public void Configure(EntityTypeBuilder<CouponRedemption>b)
 {
  b.ToTable("commerce_coupon_redemptions");b.HasKey(x=>x.Id);b.Property(x=>x.Id).HasColumnName("id").HasConversion(x=>x.Value,x=>CouponRedemptionId.From(x)).ValueGeneratedNever();b.Property(x=>x.TenantId).HasColumnName("tenant_id").HasConversion(x=>x.Value,x=>TenantId.From(x)).IsRequired();b.Property(x=>x.CouponId).HasColumnName("coupon_id").HasConversion(x=>x.Value,x=>DiscountCouponId.From(x)).IsRequired();b.Property(x=>x.OrderId).HasColumnName("order_id").HasConversion(x=>x.Value,x=>OrderId.From(x)).IsRequired();b.Property(x=>x.CustomerUserId).HasColumnName("customer_user_id").HasConversion(x=>x.Value,x=>UserId.From(x)).IsRequired();b.Property(x=>x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(18,2).IsRequired();b.Property(x=>x.RedeemedAtUtc).HasColumnName("redeemed_at_utc").IsRequired();
  b.HasIndex(x=>new{x.TenantId,x.OrderId}).IsUnique().HasDatabaseName("ux_coupon_redemptions_tenant_order");b.HasIndex(x=>new{x.TenantId,x.CouponId,x.CustomerUserId}).HasDatabaseName("ix_coupon_redemptions_coupon_customer");
  b.HasOne<DiscountCoupon>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CouponId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
 }
}
