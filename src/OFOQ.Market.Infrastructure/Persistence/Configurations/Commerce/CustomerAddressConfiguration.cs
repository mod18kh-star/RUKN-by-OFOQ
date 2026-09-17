using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> b)
    {
        b.ToTable("commerce_customer_addresses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasConversion(x => x.Value, x => CustomerAddressId.From(x)).ValueGeneratedNever();
        b.Property(x => x.TenantId).HasColumnName("tenant_id").HasConversion(x => x.Value, x => TenantId.From(x)).IsRequired();
        b.Property(x => x.UserId).HasColumnName("user_id").HasConversion(x => x.Value, x => UserId.From(x)).IsRequired();
        b.Property(x => x.Label).HasColumnName("label").HasMaxLength(CustomerAddress.MaxLabelLength).IsRequired();
        b.Property(x => x.RecipientName).HasColumnName("recipient_name").HasMaxLength(CustomerAddress.MaxNameLength).IsRequired();
        b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(CustomerAddress.MaxPhoneLength).IsRequired();
        b.Property(x => x.CountryCode).HasColumnName("country_code").HasMaxLength(CustomerAddress.MaxCountryCodeLength).IsRequired();
        b.Property(x => x.Region).HasColumnName("region").HasMaxLength(CustomerAddress.MaxRegionLength);
        b.Property(x => x.City).HasColumnName("city").HasMaxLength(CustomerAddress.MaxCityLength).IsRequired();
        b.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(CustomerAddress.MaxPostalCodeLength);
        b.Property(x => x.Line1).HasColumnName("line1").HasMaxLength(CustomerAddress.MaxAddressLineLength).IsRequired();
        b.Property(x => x.Line2).HasColumnName("line2").HasMaxLength(CustomerAddress.MaxAddressLineLength);
        b.Property(x => x.IsDefault).HasColumnName("is_default").IsRequired();
        b.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        b.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        b.HasIndex(x => new { x.TenantId, x.UserId, x.IsActive }).HasDatabaseName("ix_customer_addresses_tenant_user_active");
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
