using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class InventoryMovementConfiguration :
    IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(
        EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable(
            "catalog_inventory_movements",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_catalog_inventory_movements_before_nonnegative",
                    "quantity_before >= 0");

                table.HasCheckConstraint(
                    "ck_catalog_inventory_movements_after_nonnegative",
                    "quantity_after >= 0");

                table.HasCheckConstraint(
                    "ck_catalog_inventory_movements_delta_nonzero",
                    "quantity_delta <> 0");

                table.HasCheckConstraint(
                    "ck_catalog_inventory_movements_delta_consistent",
                    "quantity_after - quantity_before = quantity_delta");
            });

        builder.HasKey(
            movement =>
                movement.Id);

        builder.Property(
                movement =>
                    movement.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    InventoryMovementId.From(value))
            .ValueGeneratedNever();

        builder.Property(
                movement =>
                    movement.TenantId)
            .HasColumnName(
                "tenant_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    TenantId.From(value))
            .IsRequired();

        builder.Property(
                movement =>
                    movement.OrderId)
            .HasColumnName(
                "order_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    OrderId.From(value))
            .IsRequired();

        builder.Property(
                movement =>
                    movement.ProductId)
            .HasColumnName(
                "product_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductId.From(value))
            .IsRequired();

        builder.Property(
                movement =>
                    movement.ProductVariantId)
            .HasColumnName(
                "product_variant_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductVariantId.From(value))
            .IsRequired();

        builder.Property(
                movement =>
                    movement.Type)
            .HasColumnName(
                "type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                movement =>
                    movement.QuantityBefore)
            .HasColumnName(
                "quantity_before")
            .IsRequired();

        builder.Property(
                movement =>
                    movement.QuantityAfter)
            .HasColumnName(
                "quantity_after")
            .IsRequired();

        builder.Property(
                movement =>
                    movement.QuantityDelta)
            .HasColumnName(
                "quantity_delta")
            .IsRequired();

        builder.Property(
                movement =>
                    movement.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                movement =>
                    movement.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.HasIndex(
                movement =>
                    new
                    {
                        movement.TenantId,
                        movement.OrderId,
                        movement.CreatedAtUtc
                    })
            .HasDatabaseName(
                "ix_catalog_inventory_movements_order_created");

        builder.HasIndex(
                movement =>
                    new
                    {
                        movement.TenantId,
                        movement.OrderId,
                        movement.ProductVariantId,
                        movement.Type
                    })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_inventory_movements_order_variant_type");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                movement =>
                    movement.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_inventory_movements_tenants");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(
                movement =>
                    new
                    {
                        movement.TenantId,
                        movement.ProductId,
                        movement.ProductVariantId
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
                "fk_catalog_inventory_movements_variants");
    }
}