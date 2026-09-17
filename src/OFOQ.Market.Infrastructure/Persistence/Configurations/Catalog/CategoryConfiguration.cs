using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Catalog;

public sealed class CategoryConfiguration :
    IEntityTypeConfiguration<Category>
{
    public void Configure(
        EntityTypeBuilder<Category> builder)
    {
        builder.ToTable(
            "catalog_categories");

        builder.HasKey(
            category =>
                category.Id);

        builder.HasAlternateKey(
                category =>
                    new
                    {
                        category.TenantId,
                        category.Id
                    })
            .HasName(
                "ak_catalog_categories_tenant_id_id");

        builder.Property(
                category =>
                    category.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id => id.Value,
                value =>
                    CategoryId.From(value))
            .ValueGeneratedNever();

        builder.Property(
                category =>
                    category.TenantId)
            .HasColumnName(
                "tenant_id")
            .HasConversion(
                id => id.Value,
                value =>
                    TenantId.From(value))
            .IsRequired();

        builder.Property(
                category =>
                    category.Name)
            .HasColumnName(
                "name")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(
                category =>
                    category.Slug)
            .HasColumnName(
                "slug")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(
                category =>
                    category.ParentCategoryId)
            .HasColumnName(
                "parent_category_id")
            .HasConversion(
                id =>
                    id.HasValue
                        ? id.Value.Value
                        : (Guid?)null,
                value =>
                    value.HasValue
                        ? CategoryId.From(
                            value.Value)
                        : null);

        builder.Property(
                category =>
                    category.SortOrder)
            .HasColumnName(
                "sort_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(
                category =>
                    category.ImageUrl)
            .HasColumnName(
                "image_url")
            .HasMaxLength(2048);

        builder.Property(
                category =>
                    category.IsVisible)
            .HasColumnName(
                "is_visible")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(
                category =>
                    category.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                category =>
                    category.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.Property(
                category =>
                    category.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(
                category =>
                    category.UpdatedByUserId)
            .HasColumnName(
                "updated_by_user_id");

        builder.Property(
                category =>
                    category.IsDeleted)
            .HasColumnName(
                "is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(
                category =>
                    category.DeletedAtUtc)
            .HasColumnName(
                "deleted_at_utc");

        builder.Property(
                category =>
                    category.DeletedByUserId)
            .HasColumnName(
                "deleted_by_user_id");

        builder.HasIndex(
                category =>
                    category.TenantId)
            .HasDatabaseName(
                "ix_catalog_categories_tenant_id");

        builder.HasIndex(
                category =>
                    new
                    {
                        category.TenantId,
                        category.Slug
                    })
            .IsUnique()
            .HasFilter(
                "is_deleted = false")
            .HasDatabaseName(
                "ux_catalog_categories_tenant_slug_active");

        builder.HasIndex(
                category =>
                    new
                    {
                        category.TenantId,
                        category.ParentCategoryId,
                        category.SortOrder
                    })
            .HasDatabaseName(
                "ix_catalog_categories_tenant_parent_sort");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                category =>
                    category.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_categories_tenants_tenant_id");

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(
                category =>
                    category.ParentCategoryId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_categories_parent_category_id");
    }
}