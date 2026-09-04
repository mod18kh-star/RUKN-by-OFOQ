using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CartItemConfiguration :
    IEntityTypeConfiguration<CartItem>
{
    public void Configure(
        EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable(
            "commerce_cart_items");

        builder.HasKey(
            item =>
                item.Id);

        builder.Property(
                item =>
                    item.Id)
            .HasColumnName("id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    CartItemId.From(value))
            .ValueGeneratedNever();

        builder.Property(
                item =>
                    item.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    TenantId.From(value))
            .IsRequired();

        builder.Property(
                item =>
                    item.CartId)
            .HasColumnName("cart_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    CartId.From(value))
            .IsRequired();

        builder.Property(
                item =>
                    item.ProductId)
            .HasColumnName("product_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductId.From(value))
            .IsRequired();

        builder.Property(
                item =>
                    item.ProductVariantId)
            .HasColumnName(
                "product_variant_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductVariantId.From(value))
            .IsRequired();

        builder.Ignore(
            item =>
                item.UnitPrice);

        builder.Property<decimal>(
                "_unitPriceAmount")
            .HasColumnName(
                "unit_price_amount")
            .HasPrecision(
                20,
                4)
            .IsRequired();

        builder.Property<string>(
                "_unitPriceCurrencyCode")
            .HasColumnName(
                "unit_price_currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(
                item =>
                    item.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Ignore(
            item =>
                item.LineTotal);

        builder.Property(
                item =>
                    item.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                item =>
                    item.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.Property(
                item =>
                    item.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(
                item =>
                    item.UpdatedByUserId)
            .HasColumnName(
                "updated_by_user_id");

        builder.HasIndex(
                item =>
                    new
                    {
                        item.TenantId,
                        item.CartId,
                        item.ProductVariantId
                    })
            .IsUnique()
            .HasDatabaseName(
                "ux_commerce_cart_items_cart_variant");

        builder.HasIndex(
                item =>
                    new
                    {
                        item.TenantId,
                        item.CartId
                    })
            .HasDatabaseName(
                "ix_commerce_cart_items_cart");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                item =>
                    item.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_cart_items_tenants");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(
                item =>
                    new
                    {
                        item.TenantId,
                        item.ProductId
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
                "fk_commerce_cart_items_products");

        /*
         * This composite FK proves that:
         *
         * - the Variant belongs to this Tenant
         * - the Variant belongs to this Product
         *
         * So a CartItem cannot point to Product A
         * while using a Variant belonging to Product B.
         */
        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(
                item =>
                    new
                    {
                        item.TenantId,
                        item.ProductId,
                        item.ProductVariantId
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
                "fk_commerce_cart_items_variants");

        builder.ToTable(
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_commerce_cart_items_quantity",
                    "quantity > 0 AND quantity <= 999");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_cart_items_price_nonnegative",
                    "unit_price_amount >= 0");
            });
    }
}