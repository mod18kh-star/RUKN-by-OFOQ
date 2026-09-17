using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> b)
    {
        b.ToTable("commerce_customer_profiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasConversion(x => x.Value, x => CustomerProfileId.From(x)).ValueGeneratedNever();
        b.Property(x => x.TenantId).HasColumnName("tenant_id").HasConversion(x => x.Value, x => TenantId.From(x)).IsRequired();
        b.Property(x => x.UserId).HasColumnName("user_id").HasConversion(x => x.Value, x => UserId.From(x)).IsRequired();
        b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(CustomerProfile.MaxDisplayNameLength);
        b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(CustomerProfile.MaxPhoneLength);
        b.Property(x => x.MerchantNotes).HasColumnName("merchant_notes").HasMaxLength(CustomerProfile.MaxNotesLength);
        b.Property(x => x.IsBlocked).HasColumnName("is_blocked").HasDefaultValue(false).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        b.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique().HasDatabaseName("ux_customer_profiles_tenant_user");
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
