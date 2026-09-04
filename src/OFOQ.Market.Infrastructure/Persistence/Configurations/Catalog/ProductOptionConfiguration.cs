using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductOptionConfiguration :
    IEntityTypeConfiguration<ProductOption>
{
    public void Configure(
        EntityTypeBuilder<ProductOption> builder)
    {
        builder.ToTable(
            "catalog_product_options");

        builder.HasKey(
            option =>
                option.Id);

        builder.HasAlternateKey(
                option =>
                    new
                    {
                        option.TenantId,
                        option.ProductId,
                        option.Id
                    })
            .HasName(
                "ak_catalog_product_options_tenant_product_id");

        builder.Property(
                option =>
                    option.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value =>
                    ProductOptionId.From(value))
            .ValueGeneratedNever();

        builder.Property(
                option =>
                    option.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value =>
                    TenantId.From(value))
            .IsRequired();

        builder.Property(
                option =>
                    option.ProductId)
            .HasColumnName("product_id")
            .HasConversion(
                id => id.Value,
                value =>
                    ProductId.From(value))
            .IsRequired();

        builder.Property(
                option =>
                    option.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(
                option =>
                    option.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(
                option =>
                    option.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(
                option =>
                    option.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(
                option =>
                    option.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(
                option =>
                    option.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(
                option =>
                    option.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.Property(
                option =>
                    option.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(
                option =>
                    option.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

        builder.Property(
                option =>
                    option.DeletedByUserId)
            .HasColumnName("deleted_by_user_id");

        builder.HasIndex(
                option =>
                    new
                    {
                        option.TenantId,
                        option.ProductId,
                        option.NormalizedName
                    })
            .IsUnique()
            .HasFilter(
                "is_deleted = false")
            .HasDatabaseName(
                "ux_catalog_product_options_product_name_active");

        builder.HasIndex(
                option =>
                    new
                    {
                        option.TenantId,
                        option.ProductId,
                        option.SortOrder
                    })
            .HasDatabaseName(
                "ix_catalog_product_options_product_sort");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                option =>
                    option.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_product_options_tenants");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(
                option =>
                    new
                    {
                        option.TenantId,
                        option.ProductId
                    })
            .HasPrincipalKey(
                product =>
                    new
                    {
                        product.TenantId,
                        product.Id
                    })
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_product_options_products");
    }
}