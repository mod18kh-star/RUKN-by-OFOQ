using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductVariantConfiguration :
    IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(
        EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable(
            "catalog_product_variants");

        builder.HasKey(
            variant =>
                variant.Id);

        builder.Property(
                variant =>
                    variant.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductVariantId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                variant =>
                    variant.TenantId)
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
                variant =>
                    variant.ProductId)
            .HasColumnName(
                "product_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductId.From(
                        value))
            .IsRequired();

        builder.Property(
                variant =>
                    variant.Name)
            .HasColumnName(
                "name")
            .HasMaxLength(
                160)
            .IsRequired();

        /*
         * Domain-facing value objects are computed from these
         * backing fields.
         */
        builder.Ignore(
            variant =>
                variant.Sku);

        builder.Ignore(
            variant =>
                variant.PriceOverride);

        builder.Ignore(
            variant =>
                variant.Inventory);

        builder.Property<string>(
                "_skuValue")
            .HasColumnName(
                "sku")
            .HasMaxLength(
                64)
            .IsRequired();

        builder.Property<decimal?>(
                "_priceOverrideAmount")
            .HasColumnName(
                "price_override_amount")
            .HasPrecision(
                20,
                4);

        builder.Property<string?>(
                "_priceOverrideCurrencyCode")
            .HasColumnName(
                "price_override_currency_code")
            .HasMaxLength(
                3);

        builder.Property<bool>(
                "_trackInventory")
            .HasColumnName(
                "track_inventory")
            .IsRequired();

        builder.Property<int>(
                "_quantity")
            .HasColumnName(
                "quantity")
            .IsRequired();

        builder.Property<int>(
                "_lowStockThreshold")
            .HasColumnName(
                "low_stock_threshold")
            .IsRequired();

        builder.Property<bool>(
                "_continueSellingWhenOutOfStock")
            .HasColumnName(
                "continue_selling_when_out_of_stock")
            .IsRequired();

        builder.Property(
                variant =>
                    variant.IsDefault)
            .HasColumnName(
                "is_default")
            .HasDefaultValue(
                false)
            .IsRequired();

        builder.Property(
                variant =>
                    variant.IsEnabled)
            .HasColumnName(
                "is_enabled")
            .HasDefaultValue(
                true)
            .IsRequired();

        builder.Property(
                variant =>
                    variant.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                variant =>
                    variant.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.Property(
                variant =>
                    variant.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(
                variant =>
                    variant.UpdatedByUserId)
            .HasColumnName(
                "updated_by_user_id");

        builder.Property(
                variant =>
                    variant.IsDeleted)
            .HasColumnName(
                "is_deleted")
            .HasDefaultValue(
                false)
            .IsRequired();

        builder.Property(
                variant =>
                    variant.DeletedAtUtc)
            .HasColumnName(
                "deleted_at_utc");

        builder.Property(
                variant =>
                    variant.DeletedByUserId)
            .HasColumnName(
                "deleted_by_user_id");

        /*
         * SKU is unique per tenant, not globally.
         *
         * Therefore two different stores may both have
         * SKU = PHONE-BLACK-256.
         */
        builder.HasIndex(
                nameof(
                    ProductVariant.TenantId),
                "_skuValue")
            .IsUnique()
            .HasFilter(
                "is_deleted = false")
            .HasDatabaseName(
                "ux_catalog_product_variants_tenant_sku_active");

        builder.HasIndex(
                variant =>
                    new
                    {
                        variant.TenantId,
                        variant.ProductId
                    })
            .HasDatabaseName(
                "ix_catalog_product_variants_tenant_product");

        /*
         * At most one active default variant per product.
         */
        builder.HasIndex(
                nameof(
                    ProductVariant.TenantId),
                nameof(
                    ProductVariant.ProductId))
            .IsUnique()
            .HasFilter(
                "is_default = true AND is_deleted = false")
            .HasDatabaseName(
                "ux_catalog_product_variants_default_per_product");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                variant =>
                    variant.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_product_variants_tenants_tenant_id");

        /*
         * Composite tenant/product FK prevents a Variant
         * belonging to Tenant A from referencing a Product
         * belonging to Tenant B.
         */
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(
                variant =>
                    new
                    {
                        variant.TenantId,
                        variant.ProductId
                    })
            .HasPrincipalKey(
                product =>
                    new
                    {
                        product.TenantId,
                        product.Id
                    })
            .OnDelete(
                DeleteBehavior.Cascade)
            .HasConstraintName(
                "fk_catalog_product_variants_products_tenant_product");
    }
}