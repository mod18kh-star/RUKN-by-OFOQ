using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class PaymentIntentConfiguration :
    IEntityTypeConfiguration<PaymentIntent>
{
    public void Configure(EntityTypeBuilder<PaymentIntent> builder)
    {
        builder.ToTable(
            "commerce_payment_intents",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_commerce_payment_intents_amount_positive",
                    "amount > 0");
            });

        builder.HasKey(intent => intent.Id);

        builder.HasAlternateKey(intent => new
            {
                intent.TenantId,
                intent.Id
            })
            .HasName("ak_commerce_payment_intents_tenant_id_id");

        builder.Property(intent => intent.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => PaymentIntentId.From(value))
            .ValueGeneratedNever();

        builder.Property(intent => intent.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.Property(intent => intent.PaymentId)
            .HasColumnName("payment_id")
            .HasConversion(
                id => id.Value,
                value => PaymentId.From(value))
            .IsRequired();

        builder.Property(intent => intent.TenantPaymentMethodId)
            .HasColumnName("tenant_payment_method_id")
            .HasConversion(
                id => id.Value,
                value => TenantPaymentMethodId.From(value))
            .IsRequired();

        builder.Ignore(intent => intent.CustomerUserId);
        builder.Ignore(intent => intent.Currency);
        builder.Ignore(intent => intent.Total);
        builder.Ignore(intent => intent.ProviderCode);
        builder.Ignore(intent => intent.Transactions);

        builder.Property<Guid>("_customerUserId")
            .HasColumnName("customer_user_id")
            .IsRequired();

        builder.Property(intent => intent.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property<string>("_currencyCode")
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(intent => intent.MethodType)
            .HasColumnName("method_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property<string>("_providerCode")
            .HasColumnName("provider_code")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(intent => intent.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(intent => intent.ProviderReference)
            .HasColumnName("provider_reference")
            .HasMaxLength(200);

        builder.Property<string?>("CreateIdempotencyKey")
            .HasColumnName("create_idempotency_key")
            .HasMaxLength(128);

        builder.Property(intent => intent.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(intent => intent.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(intent => intent.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(intent => intent.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.HasIndex(
                nameof(PaymentIntent.TenantId),
                "_customerUserId",
                "CreateIdempotencyKey")
            .IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL")
            .HasDatabaseName("ux_commerce_payment_intents_create_idempotency");

        builder.HasIndex(intent => new
            {
                intent.TenantId,
                intent.TenantPaymentMethodId,
                intent.ProviderReference
            })
            .IsUnique()
            .HasFilter("provider_reference IS NOT NULL")
            .HasDatabaseName("ux_commerce_payment_intents_method_provider_reference");

        builder.HasIndex(intent => new
            {
                intent.TenantId,
                intent.PaymentId,
                intent.CreatedAtUtc
            })
            .HasDatabaseName("ix_commerce_payment_intents_payment_created");

        builder.HasIndex(intent => new
            {
                intent.TenantId,
                intent.Status
            })
            .HasDatabaseName("ix_commerce_payment_intents_tenant_status");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(intent => intent.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_commerce_payment_intents_tenants");

        builder.HasOne<Payment>()
            .WithMany()
            .HasForeignKey(intent => new
            {
                intent.TenantId,
                intent.PaymentId
            })
            .HasPrincipalKey(payment => new
            {
                payment.TenantId,
                payment.Id
            })
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_commerce_payment_intents_payments");

        builder.HasOne<TenantPaymentMethod>()
            .WithMany()
            .HasForeignKey(intent => new
            {
                intent.TenantId,
                intent.TenantPaymentMethodId
            })
            .HasPrincipalKey(method => new
            {
                method.TenantId,
                method.Id
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_commerce_payment_intents_tenant_payment_methods");

        builder.HasMany<PaymentTransaction>("_transactions")
            .WithOne()
            .HasForeignKey(transaction => new
            {
                transaction.TenantId,
                transaction.PaymentIntentId
            })
            .HasPrincipalKey(intent => new
            {
                intent.TenantId,
                intent.Id
            })
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_commerce_payment_transactions_intents");

        builder.Navigation("_transactions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
