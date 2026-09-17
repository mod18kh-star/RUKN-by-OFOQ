using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Tenancy;

public sealed class TenantStorefrontPresentationConfiguration :
    IEntityTypeConfiguration<TenantStorefrontPresentation>
{
    public void Configure(
        EntityTypeBuilder<TenantStorefrontPresentation> builder)
    {
        builder.ToTable(
            "tenant_storefront_presentations");

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
                    TenantStorefrontPresentationId.From(
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
                    entity.LogoUrl)
            .HasColumnName(
                "logo_url")
            .HasMaxLength(
                TenantStorefrontPresentation.MaxUrlLength);

        builder.Property(
                entity =>
                    entity.CoverImageUrl)
            .HasColumnName(
                "cover_image_url")
            .HasMaxLength(
                TenantStorefrontPresentation.MaxUrlLength);

        builder.Property(
                entity =>
                    entity.Announcement)
            .HasColumnName(
                "announcement")
            .HasMaxLength(
                TenantStorefrontPresentation.MaxAnnouncementLength);

        builder.Property(
                entity =>
                    entity.PrimaryColor)
            .HasColumnName(
                "primary_color")
            .HasMaxLength(
                7);

        builder.Property(
                entity =>
                    entity.AccentColor)
            .HasColumnName(
                "accent_color")
            .HasMaxLength(
                7);

        builder.Property(
                entity =>
                    entity.ThemePresetCode)
            .HasColumnName(
                "theme_preset_code")
            .HasMaxLength(
                TenantStorefrontPresentation.MaxCodeLength)
            .HasDefaultValue(
                TenantStorefrontPresentation.DefaultThemePresetCode)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.FontCode)
            .HasColumnName(
                "font_code")
            .HasMaxLength(
                TenantStorefrontPresentation.MaxCodeLength)
            .HasDefaultValue(
                TenantStorefrontPresentation.DefaultFontCode)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ShowCategoriesOnHome)
            .HasColumnName(
                "show_categories_on_home")
            .HasDefaultValue(
                true)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ShowProductsOnHome)
            .HasColumnName(
                "show_products_on_home")
            .HasDefaultValue(
                true)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.CategorySectionTitle)
            .HasColumnName(
                "category_section_title")
            .HasMaxLength(
                TenantStorefrontPresentation.MaxSectionTitleLength)
            .HasDefaultValue(
                TenantStorefrontPresentation.DefaultCategorySectionTitle)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ProductSectionTitle)
            .HasColumnName(
                "product_section_title")
            .HasMaxLength(
                TenantStorefrontPresentation.MaxSectionTitleLength)
            .HasDefaultValue(
                TenantStorefrontPresentation.DefaultProductSectionTitle)
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
                "ux_tenant_storefront_presentations_tenant");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                entity =>
                    entity.TenantId)
            .OnDelete(
                DeleteBehavior.Cascade)
            .HasConstraintName(
                "fk_tenant_storefront_presentations_tenants");
    }
}
