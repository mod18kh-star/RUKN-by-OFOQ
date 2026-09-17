using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Catalog;

public sealed class TenantProductRecommendationSettingsConfiguration :
    IEntityTypeConfiguration<TenantProductRecommendationSettings>
{
    public void Configure(
        EntityTypeBuilder<TenantProductRecommendationSettings> builder)
    {
        builder.ToTable(
            "catalog_recommendation_settings");

        builder.HasKey(
            settings =>
                settings.Id);

        builder.Property(
                settings =>
                    settings.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    TenantProductRecommendationSettingsId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                settings =>
                    settings.TenantId)
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
                settings =>
                    settings.IsEnabled)
            .HasColumnName(
                "is_enabled")
            .HasDefaultValue(
                true)
            .IsRequired();

        builder.Property(
                settings =>
                    settings.AutomaticSuggestionsEnabled)
            .HasColumnName(
                "automatic_suggestions_enabled")
            .HasDefaultValue(
                false)
            .IsRequired();

        builder.Property(
                settings =>
                    settings.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                settings =>
                    settings.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.Property(
                settings =>
                    settings.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(
                settings =>
                    settings.UpdatedByUserId)
            .HasColumnName(
                "updated_by_user_id");

        builder.HasIndex(
                settings =>
                    settings.TenantId)
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_recommendation_settings_tenant");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                settings =>
                    settings.TenantId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
