using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductVariantOptionValueConfiguration :
    IEntityTypeConfiguration<ProductVariantOptionValue>
{
    public void Configure(
        EntityTypeBuilder<ProductVariantOptionValue> builder)
    {
        builder.ToTable(
            "catalog_product_variant_option_values");

        builder.HasKey(
            assignment =>
                assignment.Id);

        builder.Property(
                assignment =>
                    assignment.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                raw =>
                    ProductVariantOptionValueId.From(raw))
            .ValueGeneratedNever();

        builder.Property(
                assignment =>
                    assignment.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                raw =>
                    TenantId.From(raw))
            .IsRequired();

        builder.Property(
                assignment =>
                    assignment.ProductId)
            .HasColumnName("product_id")
            .HasConversion(
                id => id.Value,
                raw =>
                    ProductId.From(raw))
            .IsRequired();

        builder.Property(
                assignment =>
                    assignment.ProductVariantId)
            .HasColumnName("product_variant_id")
            .HasConversion(
                id => id.Value,
                raw =>
                    ProductVariantId.From(raw))
            .IsRequired();

        builder.Property(
                assignment =>
                    assignment.ProductOptionId)
            .HasColumnName("product_option_id")
            .HasConversion(
                id => id.Value,
                raw =>
                    ProductOptionId.From(raw))
            .IsRequired();

        builder.Property(
                assignment =>
                    assignment.ProductOptionValueId)
            .HasColumnName("product_option_value_id")
            .HasConversion(
                id => id.Value,
                raw =>
                    ProductOptionValueId.From(raw))
            .IsRequired();

        builder.Property(
                assignment =>
                    assignment.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(
                assignment =>
                    assignment.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(
                assignment =>
                    assignment.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(
                assignment =>
                    assignment.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.Property(
                assignment =>
                    assignment.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(
                assignment =>
                    assignment.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

        builder.Property(
                assignment =>
                    assignment.DeletedByUserId)
            .HasColumnName("deleted_by_user_id");

        /*
         * One Variant may choose only one active Value
         * from a given Option.
         *
         * Example:
         * Color = Black OR White, never both.
         */
        builder.HasIndex(
                assignment =>
                    new
                    {
                        assignment.TenantId,
                        assignment.ProductId,
                        assignment.ProductVariantId,
                        assignment.ProductOptionId
                    })
            .IsUnique()
            .HasFilter(
                "is_deleted = false")
            .HasDatabaseName(
                "ux_catalog_variant_option_single_value_active");

        builder.HasIndex(
                assignment =>
                    new
                    {
                        assignment.TenantId,
                        assignment.ProductId,
                        assignment.ProductVariantId
                    })
            .HasDatabaseName(
                "ix_catalog_variant_option_values_variant");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                assignment =>
                    assignment.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_variant_option_values_tenants");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(
                assignment =>
                    new
                    {
                        assignment.TenantId,
                        assignment.ProductId
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
                "fk_catalog_variant_option_values_products");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(
                assignment =>
                    new
                    {
                        assignment.TenantId,
                        assignment.ProductId,
                        assignment.ProductVariantId
                    })
            .HasPrincipalKey(
                variant =>
                    new
                    {
                        variant.TenantId,
                        variant.ProductId,
                        variant.Id
                    })
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_variant_option_values_variants");

        builder.HasOne<ProductOption>()
            .WithMany()
            .HasForeignKey(
                assignment =>
                    new
                    {
                        assignment.TenantId,
                        assignment.ProductId,
                        assignment.ProductOptionId
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
                "fk_catalog_variant_option_values_options");

        /*
         * This composite FK is important:
         *
         * it proves the selected Value actually belongs to
         * the same Product + Option referenced above.
         */
        builder.HasOne<ProductOptionValue>()
            .WithMany()
            .HasForeignKey(
                assignment =>
                    new
                    {
                        assignment.TenantId,
                        assignment.ProductId,
                        assignment.ProductOptionId,
                        assignment.ProductOptionValueId
                    })
            .HasPrincipalKey(
                value =>
                    new
                    {
                        value.TenantId,
                        value.ProductId,
                        value.ProductOptionId,
                        value.Id
                    })
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_variant_option_values_values");
    }
}