using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductOptionValueConfiguration :
    IEntityTypeConfiguration<ProductOptionValue>
{
    public void Configure(
        EntityTypeBuilder<ProductOptionValue> builder)
    {
        builder.ToTable(
            "catalog_product_option_values");

        builder.HasKey(
            value =>
                value.Id);

        builder.HasAlternateKey(
                value =>
                    new
                    {
                        value.TenantId,
                        value.ProductId,
                        value.ProductOptionId,
                        value.Id
                    })
            .HasName(
                "ak_catalog_product_option_values_scope_id");

        builder.Property(
                value =>
                    value.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                raw =>
                    ProductOptionValueId.From(raw))
            .ValueGeneratedNever();

        builder.Property(
                value =>
                    value.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                raw =>
                    TenantId.From(raw))
            .IsRequired();

        builder.Property(
                value =>
                    value.ProductId)
            .HasColumnName("product_id")
            .HasConversion(
                id => id.Value,
                raw =>
                    ProductId.From(raw))
            .IsRequired();

        builder.Property(
                value =>
                    value.ProductOptionId)
            .HasColumnName("product_option_id")
            .HasConversion(
                id => id.Value,
                raw =>
                    ProductOptionId.From(raw))
            .IsRequired();

        builder.Property(
                value =>
                    value.Value)
            .HasColumnName("value")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(
                value =>
                    value.NormalizedValue)
            .HasColumnName("normalized_value")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(
                value =>
                    value.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(
                value =>
                    value.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(
                value =>
                    value.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(
                value =>
                    value.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(
                value =>
                    value.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.Property(
                value =>
                    value.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(
                value =>
                    value.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

        builder.Property(
                value =>
                    value.DeletedByUserId)
            .HasColumnName("deleted_by_user_id");

        builder.HasIndex(
                value =>
                    new
                    {
                        value.TenantId,
                        value.ProductId,
                        value.ProductOptionId,
                        value.NormalizedValue
                    })
            .IsUnique()
            .HasFilter(
                "is_deleted = false")
            .HasDatabaseName(
                "ux_catalog_product_option_values_option_value_active");

        builder.HasIndex(
                value =>
                    new
                    {
                        value.TenantId,
                        value.ProductOptionId,
                        value.SortOrder
                    })
            .HasDatabaseName(
                "ix_catalog_product_option_values_option_sort");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                value =>
                    value.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_product_option_values_tenants");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(
                value =>
                    new
                    {
                        value.TenantId,
                        value.ProductId
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
                "fk_catalog_product_option_values_products");

        builder.HasOne<ProductOption>()
            .WithMany()
            .HasForeignKey(
                value =>
                    new
                    {
                        value.TenantId,
                        value.ProductId,
                        value.ProductOptionId
                    })
            .HasPrincipalKey(
                option =>
                    new
                    {
                        option.TenantId,
                        option.ProductId,
                        option.Id
                    })
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_product_option_values_options");
    }
}