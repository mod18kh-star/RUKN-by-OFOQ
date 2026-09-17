using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Tenancy;

public sealed class TenantStoreSocialLinkConfiguration :
    IEntityTypeConfiguration<TenantStoreSocialLink>
{
    public void Configure(
        EntityTypeBuilder<TenantStoreSocialLink> builder)
    {
        builder.ToTable(
            "tenant_store_social_links",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_tenant_store_social_links_sort_order",
                    "sort_order >= 0");
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
                    TenantStoreSocialLinkId.From(
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
                    entity.PlatformCode)
            .HasColumnName(
                "platform_code")
            .HasMaxLength(
                TenantStoreSocialLink.MaxPlatformCodeLength)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.Label)
            .HasColumnName(
                "label")
            .HasMaxLength(
                TenantStoreSocialLink.MaxLabelLength);

        builder.Property(
                entity =>
                    entity.Url)
            .HasColumnName(
                "url")
            .HasMaxLength(
                TenantStoreSocialLink.MaxUrlLength)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.SortOrder)
            .HasColumnName(
                "sort_order")
            .IsRequired();

        builder.Property(
                entity =>
                    entity.IsVisible)
            .HasColumnName(
                "is_visible")
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
                        entity.SortOrder
                    })
            .HasDatabaseName(
                "ix_tenant_store_social_links_tenant_sort");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                entity =>
                    entity.TenantId)
            .OnDelete(
                DeleteBehavior.Cascade)
            .HasConstraintName(
                "fk_tenant_store_social_links_tenants");
    }
}
