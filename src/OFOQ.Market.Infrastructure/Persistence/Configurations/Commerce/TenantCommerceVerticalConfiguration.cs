using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

internal sealed class TenantCommerceVerticalConfiguration :
    IEntityTypeConfiguration<TenantCommerceVertical>
{
    public void Configure(
        EntityTypeBuilder<TenantCommerceVertical> builder)
    {
        builder.ToTable(
            "commerce_tenant_verticals",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_commerce_verticals_vertical_type",
                    "vertical_type > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_verticals_primary_enabled",
                    "NOT is_primary OR is_enabled");
            });

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
                    TenantCommerceVerticalId.From(
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
                    entity.VerticalType)
            .HasColumnName(
                "vertical_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                entity =>
                    entity.IsEnabled)
            .HasColumnName(
                "is_enabled")
            .IsRequired();

        builder.Property(
                entity =>
                    entity.IsPrimary)
            .HasColumnName(
                "is_primary")
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
                    new
                    {
                        entity.TenantId,
                        entity.VerticalType
                    })
            .IsUnique()
            .HasDatabaseName(
                "ux_commerce_verticals_tenant_vertical");

        builder.HasIndex(
                entity =>
                    entity.TenantId)
            .IsUnique()
            .HasDatabaseName(
                "ux_commerce_verticals_tenant_primary")
            .HasFilter(
                "is_primary = TRUE");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                entity =>
                    entity.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_verticals_tenants");
    }
}