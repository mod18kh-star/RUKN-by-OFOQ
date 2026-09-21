using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Tenancy;

public sealed class TenantStoreProfileConfiguration :
    IEntityTypeConfiguration<TenantStoreProfile>
{
    public void Configure(
        EntityTypeBuilder<TenantStoreProfile> builder)
    {
        builder.ToTable(
            "tenant_store_profiles");

        builder.HasKey(
            entity =>
                entity.Id);

        builder.Property(
                entity =>
                    entity.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    TenantStoreProfileId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                entity =>
                    entity.TenantId)
            .HasColumnName(
                "tenant_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    TenantId.From(
                        value))
            .IsRequired();

        builder.Property(
                entity =>
                    entity.WebsiteUrl)
            .HasColumnName(
                "website_url")
            .HasMaxLength(
                TenantStoreProfile.MaxWebsiteUrlLength);

        builder.Property(
                entity =>
                    entity.WhatsAppNumber)
            .HasColumnName(
                "whatsapp_number")
            .HasMaxLength(
                TenantStoreProfile.MaxPhoneLength);

        builder.Property(
                entity =>
                    entity.CustomerServicePhone)
            .HasColumnName(
                "customer_service_phone")
            .HasMaxLength(
                TenantStoreProfile.MaxPhoneLength);

        builder.Property(
                entity =>
                    entity.SecondaryPhone)
            .HasColumnName(
                "secondary_phone")
            .HasMaxLength(
                TenantStoreProfile.MaxPhoneLength);

        builder.Property(
                entity =>
                    entity.LandlinePhone)
            .HasColumnName(
                "landline_phone")
            .HasMaxLength(
                TenantStoreProfile.MaxPhoneLength);

        builder.Property(
                entity =>
                    entity.PhysicalAddress)
            .HasColumnName(
                "physical_address")
            .HasMaxLength(
                TenantStoreProfile.MaxPhysicalAddressLength);

        builder.Property(
                entity =>
                    entity.GoogleMapsUrl)
            .HasColumnName(
                "google_maps_url")
            .HasMaxLength(
                TenantStoreProfile.MaxGoogleMapsUrlLength);

        builder.Property(
                entity =>
                    entity.CommercialRegistrationNumber)
            .HasColumnName(
                "commercial_registration_number")
            .HasMaxLength(
                TenantStoreProfile.MaxCommercialRegistrationLength);

        builder.Property(
                entity =>
                    entity.CommercialRegistrationNotApplicable)
            .HasColumnName(
                "commercial_registration_not_applicable")
            .HasDefaultValue(
                false)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ShowWebsite)
            .HasColumnName(
                "show_website")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ShowWhatsApp)
            .HasColumnName(
                "show_whatsapp")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ShowCustomerServicePhone)
            .HasColumnName(
                "show_customer_service_phone")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ShowSecondaryPhone)
            .HasColumnName(
                "show_secondary_phone")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ShowLandlinePhone)
            .HasColumnName(
                "show_landline_phone")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ShowPhysicalAddress)
            .HasColumnName(
                "show_physical_address")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ShowCommercialRegistration)
            .HasColumnName(
                "show_commercial_registration")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                entity =>
                    entity.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.Property(
                entity =>
                    entity.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(
                entity =>
                    entity.UpdatedByUserId)
            .HasColumnName(
                "updated_by_user_id");

        builder.HasIndex(
                entity =>
                    entity.TenantId)
            .IsUnique()
            .HasDatabaseName(
                "ux_tenant_store_profiles_tenant");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                entity =>
                    entity.TenantId)
            .OnDelete(
                DeleteBehavior.Cascade)
            .HasConstraintName(
                "fk_tenant_store_profiles_tenants");
    }
}
