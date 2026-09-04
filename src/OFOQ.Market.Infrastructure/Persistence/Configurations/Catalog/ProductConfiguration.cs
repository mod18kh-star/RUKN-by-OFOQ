using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductConfiguration :
    IEntityTypeConfiguration<Product>
{
    public void Configure(
        EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(
            "catalog_products");

        builder.HasKey(
            product =>
                product.Id);

        builder.HasAlternateKey(
                product =>
                    new
                    {
                        product.TenantId,
                        product.Id
                    })
            .HasName(
                "ak_catalog_products_tenant_id_id");

        builder.Property(
                product =>
                    product.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                product =>
                    product.TenantId)
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
                product =>
                    product.Name)
            .HasColumnName(
                "name")
            .HasMaxLength(
                200)
            .IsRequired();

        builder.Property(
                product =>
                    product.Slug)
            .HasColumnName(
                "slug")
            .HasMaxLength(
                160)
            .IsRequired();

        builder.Property(
                product =>
                    product.Description)
            .HasColumnName(
                "description")
            .HasMaxLength(
                5000);

        builder.Property(
                product =>
                    product.CategoryId)
            .HasColumnName(
                "category_id")
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

        /*
         * Price and CompareAtPrice are domain-facing computed
         * value objects.
         *
         * EF persists their backing fields directly.
         */
        builder.Ignore(
            product =>
                product.Price);

        builder.Ignore(
            product =>
                product.CompareAtPrice);

        builder.Property<decimal>(
                "_priceAmount")
            .HasColumnName(
                "price_amount")
            .HasPrecision(
                20,
                4)
            .IsRequired();

        builder.Property<string>(
                "_currencyCode")
            .HasColumnName(
                "currency_code")
            .HasMaxLength(
                3)
            .IsRequired();

        builder.Property<decimal?>(
                "_compareAtPriceAmount")
            .HasColumnName(
                "compare_at_price_amount")
            .HasPrecision(
                20,
                4);

        builder.Property(
                product =>
                    product.Status)
            .HasColumnName(
                "status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                product =>
                    product.IsVisible)
            .HasColumnName(
                "is_visible")
            .HasDefaultValue(
                false)
            .IsRequired();

        builder.Property(
                product =>
                    product.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                product =>
                    product.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.Property(
                product =>
                    product.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(
                product =>
                    product.UpdatedByUserId)
            .HasColumnName(
                "updated_by_user_id");

        builder.Property(
                product =>
                    product.IsDeleted)
            .HasColumnName(
                "is_deleted")
            .HasDefaultValue(
                false)
            .IsRequired();

        builder.Property(
                product =>
                    product.DeletedAtUtc)
            .HasColumnName(
                "deleted_at_utc");

        builder.Property(
                product =>
                    product.DeletedByUserId)
            .HasColumnName(
                "deleted_by_user_id");

        builder.HasIndex(
                product =>
                    product.TenantId)
            .HasDatabaseName(
                "ix_catalog_products_tenant_id");

        builder.HasIndex(
                product =>
                    new
                    {
                        product.TenantId,
                        product.Slug
                    })
            .IsUnique()
            .HasFilter(
                "is_deleted = false")
            .HasDatabaseName(
                "ux_catalog_products_tenant_slug_active");

        builder.HasIndex(
                product =>
                    new
                    {
                        product.TenantId,
                        product.CategoryId
                    })
            .HasDatabaseName(
                "ix_catalog_products_tenant_category");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                product =>
                    product.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_products_tenants_tenant_id");

        /*
         * TenantId is part of the FK deliberately.
         *
         * This prevents a product belonging to Tenant A
         * from referencing a Category belonging to Tenant B
         * even at the database constraint level.
         */
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(
                product =>
                    new
                    {
                        product.TenantId,
                        product.CategoryId
                    })
            .HasPrincipalKey(
                category =>
                    new
                    {
                        category.TenantId,
                        category.Id
                    })
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_products_categories_tenant_category");
    }
}