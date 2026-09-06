using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class PaymentConfiguration :
    IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable(
            "commerce_payments",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_commerce_payments_amount_positive",
                    "amount > 0");
            });

        builder.HasKey(payment => payment.Id);

        builder.HasAlternateKey(payment => new
            {
                payment.TenantId,
                payment.Id
            })
            .HasName("ak_commerce_payments_tenant_id_id");

        builder.Property(payment => payment.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => PaymentId.From(value))
            .ValueGeneratedNever();

        builder.Property(payment => payment.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.Property(payment => payment.OrderId)
            .HasColumnName("order_id")
            .HasConversion(
                id => id.Value,
                value => OrderId.From(value))
            .IsRequired();

        builder.Ignore(payment => payment.CustomerUserId);
        builder.Ignore(payment => payment.Currency);
        builder.Ignore(payment => payment.Total);

        builder.Property<Guid>("_customerUserId")
            .HasColumnName("customer_user_id")
            .IsRequired();

        builder.Property(payment => payment.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property<string>("_currencyCode")
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(payment => payment.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(payment => payment.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(payment => payment.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(payment => payment.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(payment => payment.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.HasIndex(payment => new
            {
                payment.TenantId,
                payment.OrderId
            })
            .IsUnique()
            .HasDatabaseName("ux_commerce_payments_tenant_order");

        builder.HasIndex(
                nameof(Payment.TenantId),
                "_customerUserId",
                nameof(Payment.CreatedAtUtc))
            .HasDatabaseName("ix_commerce_payments_tenant_customer_created");

        builder.HasIndex(payment => new
            {
                payment.TenantId,
                payment.Status
            })
            .HasDatabaseName("ix_commerce_payments_tenant_status");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(payment => payment.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_commerce_payments_tenants");

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(payment => new
            {
                payment.TenantId,
                payment.OrderId
            })
            .HasPrincipalKey(order => new
            {
                order.TenantId,
                order.Id
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_commerce_payments_orders");
    }
}
