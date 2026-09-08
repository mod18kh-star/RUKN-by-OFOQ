using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

internal sealed class TenantCommerceCapabilityOverrideConfiguration :
    IEntityTypeConfiguration<TenantCommerceCapabilityOverride>
{
    public void Configure(
        EntityTypeBuilder<TenantCommerceCapabilityOverride> builder)
    {
        builder.ToTable(
            "commerce_tenant_capability_overrides",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_commerce_cap_overrides_capability_type",
                    "capability_type > 0");
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
                    TenantCommerceCapabilityOverrideId.From(
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
                    entity.CapabilityType)
            .HasColumnName(
                "capability_type")
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
                        entity.CapabilityType
                    })
            .IsUnique()
            .HasDatabaseName(
                "ux_commerce_cap_overrides_tenant_capability");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                entity =>
                    entity.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_cap_overrides_tenants");
    }
}