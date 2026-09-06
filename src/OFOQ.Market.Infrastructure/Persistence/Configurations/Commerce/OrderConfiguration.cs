using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class OrderConfiguration :
    IEntityTypeConfiguration<Order>
{
    public void Configure(
        EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(
            "commerce_orders");

        builder.HasKey(
            order =>
                order.Id);

        builder.HasAlternateKey(
                order =>
                    new
                    {
                        order.TenantId,
                        order.Id
                    })
            .HasName(
                "ak_commerce_orders_tenant_id_id");

        builder.Property(
                order =>
                    order.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    OrderId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                order =>
                    order.TenantId)
            .HasColumnName(
                "tenant_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    TenantId.From(
                        value))
            .IsRequired();

        builder.Ignore(
            order =>
                order.CustomerUserId);

        builder.Ignore(
            order =>
                order.SourceCartId);

        builder.Ignore(
            order =>
                order.Currency);

        builder.Ignore(
            order =>
                order.Items);

        builder.Property<Guid>(
                "_customerUserId")
            .HasColumnName(
                "customer_user_id")
            .IsRequired();

        builder.Property<CartId>(
                "_sourceCartId")
            .HasColumnName(
                "source_cart_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    CartId.From(
                        value))
            .IsRequired();

        builder.Property<string>(
                "_currencyCode")
            .HasColumnName(
                "currency_code")
            .HasMaxLength(
                3)
            .IsRequired();

        /*
         * HTTP idempotency is persistence metadata, not
         * Order domain behavior. Existing orders remain
         * valid with a null value; every checkout-created
         * order writes a key through OrderRepository.
         */
        builder.Property<string?>(
                "CheckoutIdempotencyKey")
            .HasColumnName(
                "checkout_idempotency_key")
            .HasMaxLength(
                128);

        builder.Property(
                order =>
                    order.Status)
            .HasColumnName(
                "status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                order =>
                    order.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                order =>
                    order.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.Property(
                order =>
                    order.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(
                order =>
                    order.UpdatedByUserId)
            .HasColumnName(
                "updated_by_user_id");

        /*
         * A cart may produce at most one order.
         */
        builder.HasIndex(
                nameof(
                    Order.TenantId),
                "_sourceCartId")
            .IsUnique()
            .HasDatabaseName(
                "ux_commerce_orders_tenant_source_cart");

        builder.HasIndex(
                nameof(
                    Order.TenantId),
                "_customerUserId",
                nameof(
                    Order.CreatedAtUtc))
            .HasDatabaseName(
                "ix_commerce_orders_tenant_customer_created");

        builder.HasIndex(
                nameof(
                    Order.TenantId),
                "_customerUserId",
                "CheckoutIdempotencyKey")
            .IsUnique()
            .HasFilter(
                "checkout_idempotency_key IS NOT NULL")
            .HasDatabaseName(
                "ux_commerce_orders_checkout_idempotency");

        builder.HasIndex(
                order =>
                    new
                    {
                        order.TenantId,
                        order.Status
                    })
            .HasDatabaseName(
                "ix_commerce_orders_tenant_status");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                order =>
                    order.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_orders_tenants");

        /*
         * Order and source Cart must always belong
         * to the same tenant.
         *
         * Both sides now use:
         *
         * TenantId + CartId
         */
        builder.HasOne<Cart>()
            .WithMany()
            .HasForeignKey(
                nameof(
                    Order.TenantId),
                "_sourceCartId")
            .HasPrincipalKey(
                nameof(
                    Cart.TenantId),
                nameof(
                    Cart.Id))
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_orders_source_cart");

        builder.HasMany<OrderItem>(
                "_items")
            .WithOne()
            .HasForeignKey(
                item =>
                    new
                    {
                        item.TenantId,
                        item.OrderId
                    })
            .HasPrincipalKey(
                order =>
                    new
                    {
                        order.TenantId,
                        order.Id
                    })
            .OnDelete(
                DeleteBehavior.Cascade)
            .HasConstraintName(
                "fk_commerce_order_items_orders");

        builder.Navigation(
                "_items")
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}