using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Fulfillment;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class ShippingMethodConfiguration : IEntityTypeConfiguration<ShippingMethod>
{
    public void Configure(EntityTypeBuilder<ShippingMethod> b)
    {
        b.ToTable("commerce_shipping_methods");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasConversion(x => x.Value, x => ShippingMethodId.From(x)).ValueGeneratedNever();
        b.Property(x => x.TenantId).HasColumnName("tenant_id").HasConversion(x => x.Value, x => TenantId.From(x)).IsRequired();
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(ShippingMethod.MaxCodeLength).IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(ShippingMethod.MaxNameLength).IsRequired();
        b.Property(x => x.Type).HasColumnName("type").HasConversion<int>().IsRequired();
        b.Property(x => x.Price).HasColumnName("price").HasPrecision(18,2).IsRequired();
        b.Property(x => x.Currency).HasColumnName("currency_code").HasConversion(x => x.Value, x => CurrencyCode.Create(x)).HasMaxLength(3).IsRequired();
        b.Property(x => x.MinimumOrderAmount).HasColumnName("minimum_order_amount").HasPrecision(18,2);
        b.Property(x => x.MaximumOrderAmount).HasColumnName("maximum_order_amount").HasPrecision(18,2);
        b.Property(x => x.PickupLocationId).HasColumnName("pickup_location_id").HasConversion(x => x.HasValue ? x.Value.Value : (Guid?)null, x => x.HasValue ? FulfillmentLocationId.From(x.Value) : null);
        b.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        b.Property(x => x.IsEnabled).HasColumnName("is_enabled").IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        b.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasDatabaseName("ux_shipping_methods_tenant_code");
        b.HasIndex(x => new { x.TenantId, x.IsEnabled, x.SortOrder }).HasDatabaseName("ix_shipping_methods_tenant_enabled_sort");
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<FulfillmentLocation>().WithMany().HasForeignKey(x => x.PickupLocationId).OnDelete(DeleteBehavior.Restrict);
    }
}
