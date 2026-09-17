using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Discounts;
using OFOQ.Market.Domain.Tenancy;
namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;
public sealed class DiscountCouponConfiguration : IEntityTypeConfiguration<DiscountCoupon>
{
    public void Configure(EntityTypeBuilder<DiscountCoupon> b)
    {
        b.ToTable("commerce_discount_coupons"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasConversion(x => x.Value, x => DiscountCouponId.From(x)).ValueGeneratedNever();
        b.Property(x => x.TenantId).HasColumnName("tenant_id").HasConversion(x => x.Value, x => TenantId.From(x)).IsRequired();
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(DiscountCoupon.MaxCodeLength).IsRequired(); b.Property(x => x.Name).HasColumnName("name").HasMaxLength(DiscountCoupon.MaxNameLength).IsRequired();
        b.Property(x => x.Type).HasColumnName("type").HasConversion<int>().IsRequired(); b.Property(x => x.Value).HasColumnName("value").HasPrecision(18,2).IsRequired(); b.Property(x => x.Scope).HasColumnName("scope").HasConversion<int>().IsRequired();
        b.Ignore(x => x.Currency); b.Property<string>("_currencyCode").HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        b.Property(x => x.IncludeDescendantCategories).HasColumnName("include_descendant_categories").IsRequired(); b.Property(x => x.MinimumOrderAmount).HasColumnName("minimum_order_amount").HasPrecision(18,2);
        b.Property(x => x.MaximumTotalUses).HasColumnName("maximum_total_uses"); b.Property(x => x.MaximumUsesPerCustomer).HasColumnName("maximum_uses_per_customer"); b.Property(x => x.StartsAtUtc).HasColumnName("starts_at_utc"); b.Property(x => x.EndsAtUtc).HasColumnName("ends_at_utc"); b.Property(x => x.IsEnabled).HasColumnName("is_enabled").IsRequired();
        b.Ignore(x => x.ProductTargets); b.Ignore(x => x.CategoryTargets);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired(); b.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id"); b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc"); b.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasDatabaseName("ux_discount_coupons_tenant_code");
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany<DiscountCouponProductTarget>("_productTargets").WithOne().HasForeignKey(x => new { x.TenantId, x.CouponId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        b.HasMany<DiscountCouponCategoryTarget>("_categoryTargets").WithOne().HasForeignKey(x => new { x.TenantId, x.CouponId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        b.HasAlternateKey(x => new { x.TenantId, x.Id });
    }
}
