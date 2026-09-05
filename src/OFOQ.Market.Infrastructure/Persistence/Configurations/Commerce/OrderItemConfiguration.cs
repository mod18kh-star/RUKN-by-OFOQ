using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class OrderItemConfiguration :
    IEntityTypeConfiguration<OrderItem>
{
    public void Configure(
        EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable(
            "commerce_order_items",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_commerce_order_items_quantity",
                    "quantity > 0 AND quantity <= 999");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_order_items_price_nonnegative",
                    "unit_price_amount >= 0");
            });

        builder.HasKey(
            item =>
                item.Id);

        builder.Property(
                item =>
                    item.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    OrderItemId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                item =>
                    item.TenantId)
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
                item =>
                    item.OrderId)
            .HasColumnName(
                "order_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    OrderId.From(
                        value))
            .IsRequired();

        builder.Property(
                item =>
                    item.ProductId)
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
                item =>
                    item.ProductVariantId)
            .HasColumnName(
                "product_variant_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductVariantId.From(
                        value))
            .IsRequired();

        builder.Property(
                item =>
                    item.ProductName)
            .HasColumnName(
                "product_name")
            .HasMaxLength(
                200)
            .IsRequired();

        builder.Property(
                item =>
                    item.VariantName)
            .HasColumnName(
                "variant_name")
            .HasMaxLength(
                160)
            .IsRequired();

        builder.Property(
                item =>
                    item.Sku)
            .HasColumnName(
                "sku")
            .HasMaxLength(
                64)
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
            .HasMaxLength(
                3)
            .IsRequired();

        builder.Property(
                item =>
                    item.Quantity)
            .HasColumnName(
                "quantity")
            .IsRequired();

        /*
         * The aggregate already prevents duplicate variants.
         * Keep the same invariant at database level.
         */
        builder.HasIndex(
                item =>
                    new
                    {
                        item.TenantId,
                        item.OrderId,
                        item.ProductVariantId
                    })
            .IsUnique()
            .HasDatabaseName(
                "ux_commerce_order_items_order_variant");

        builder.HasIndex(
                item =>
                    new
                    {
                        item.TenantId,
                        item.OrderId
                    })
            .HasDatabaseName(
                "ix_commerce_order_items_order");

        builder.HasIndex(
                item =>
                    new
                    {
                        item.TenantId,
                        item.ProductId,
                        item.ProductVariantId
                    })
            .HasDatabaseName(
                "ix_commerce_order_items_product_variant");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                item =>
                    item.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_order_items_tenants");

        /*
         * Product/variant IDs remain useful historical
         * references while the snapshot stores the
         * immutable customer-facing data.
         */
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
                "fk_commerce_order_items_products");

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
                "fk_commerce_order_items_variants");
    }
}