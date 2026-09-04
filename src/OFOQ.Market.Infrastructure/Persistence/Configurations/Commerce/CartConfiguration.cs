using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CartConfiguration :
    IEntityTypeConfiguration<Cart>
{
    public void Configure(
        EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable(
            "commerce_carts");

        builder.HasKey(
            cart =>
                cart.Id);

        builder.HasAlternateKey(
                cart =>
                    new
                    {
                        cart.TenantId,
                        cart.Id
                    })
            .HasName(
                "ak_commerce_carts_tenant_id_id");

        builder.Property(
                cart =>
                    cart.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value =>
                    CartId.From(value))
            .ValueGeneratedNever();

        builder.Property(
                cart =>
                    cart.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value =>
                    TenantId.From(value))
            .IsRequired();

        builder.Ignore(
            cart =>
                cart.CustomerUserId);

        builder.Property<Guid?>(
                "_customerUserId")
            .HasColumnName(
                "customer_user_id");

        builder.Property(
                cart =>
                    cart.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Ignore(
            cart =>
                cart.Currency);

        builder.Property<string?>(
                "_currencyCode")
            .HasColumnName(
                "currency_code")
            .HasMaxLength(3);

        builder.Ignore(
            cart =>
                cart.TotalQuantity);

        builder.Ignore(
            cart =>
                cart.TotalAmount);

        builder.Ignore(
            cart =>
                cart.Items);

        builder.Property(
                cart =>
                    cart.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                cart =>
                    cart.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.Property(
                cart =>
                    cart.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(
                cart =>
                    cart.UpdatedByUserId)
            .HasColumnName(
                "updated_by_user_id");

        builder.HasIndex(
                cart =>
                    new
                    {
                        cart.TenantId,
                        cart.Status
                    })
            .HasDatabaseName(
                "ix_commerce_carts_tenant_status");

        /*
         * One active cart per authenticated customer
         * inside each tenant.
         *
         * Guest carts have NULL customer_user_id and
         * therefore are not constrained by this index.
         */
        builder.HasIndex(
                "TenantId",
                "_customerUserId")
            .IsUnique()
            .HasFilter(
                "status = 0 AND customer_user_id IS NOT NULL")
            .HasDatabaseName(
                "ux_commerce_carts_active_customer");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                cart =>
                    cart.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_carts_tenants");

        builder.HasMany<CartItem>(
                "_items")
            .WithOne()
            .HasForeignKey(
                item =>
                    new
                    {
                        item.TenantId,
                        item.CartId
                    })
            .HasPrincipalKey(
                cart =>
                    new
                    {
                        cart.TenantId,
                        cart.Id
                    })
            .OnDelete(
                DeleteBehavior.Cascade)
            .HasConstraintName(
                "fk_commerce_cart_items_carts");

        builder.Navigation(
                "_items")
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}