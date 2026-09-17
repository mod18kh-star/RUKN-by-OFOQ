using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Fulfillment;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class FulfillmentLocationConfiguration : IEntityTypeConfiguration<FulfillmentLocation>
{
    public void Configure(EntityTypeBuilder<FulfillmentLocation> b)
    {
        b.ToTable("commerce_fulfillment_locations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasConversion(x => x.Value, x => FulfillmentLocationId.From(x)).ValueGeneratedNever();
        b.Property(x => x.TenantId).HasColumnName("tenant_id").HasConversion(x => x.Value, x => TenantId.From(x)).IsRequired();
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(FulfillmentLocation.MaxCodeLength).IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(FulfillmentLocation.MaxNameLength).IsRequired();
        b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(FulfillmentLocation.MaxPhoneLength);
        b.Property(x => x.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsRequired();
        b.Property(x => x.City).HasColumnName("city").HasMaxLength(FulfillmentLocation.MaxNameLength).IsRequired();
        b.Property(x => x.Region).HasColumnName("region").HasMaxLength(FulfillmentLocation.MaxNameLength);
        b.Property(x => x.Line1).HasColumnName("line1").HasMaxLength(FulfillmentLocation.MaxAddressLength).IsRequired();
        b.Property(x => x.Line2).HasColumnName("line2").HasMaxLength(FulfillmentLocation.MaxAddressLength);
        b.Property(x => x.IsDefault).HasColumnName("is_default").IsRequired();
        b.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        b.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasDatabaseName("ux_fulfillment_locations_tenant_code");
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
