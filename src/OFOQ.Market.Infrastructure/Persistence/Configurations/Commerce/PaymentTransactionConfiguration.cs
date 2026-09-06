using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class PaymentTransactionConfiguration :
    IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable(
            "commerce_payment_transactions",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_commerce_payment_transactions_amount_positive",
                    "amount > 0");
            });

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => PaymentTransactionId.From(value))
            .ValueGeneratedNever();

        builder.Property(transaction => transaction.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.Property(transaction => transaction.PaymentIntentId)
            .HasColumnName("payment_intent_id")
            .HasConversion(
                id => id.Value,
                value => PaymentIntentId.From(value))
            .IsRequired();

        builder.Property(transaction => transaction.Type)
            .HasColumnName("type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(transaction => transaction.ResultingStatus)
            .HasColumnName("resulting_status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(transaction => transaction.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Ignore(transaction => transaction.Currency);

        builder.Property<string>("_currencyCode")
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(transaction => transaction.ProviderReference)
            .HasColumnName("provider_reference")
            .HasMaxLength(200);

        builder.Property(transaction => transaction.ExternalEventId)
            .HasColumnName("external_event_id")
            .HasMaxLength(200);

        builder.Property(transaction => transaction.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(transaction => new
            {
                transaction.TenantId,
                transaction.PaymentIntentId,
                transaction.CreatedAtUtc
            })
            .HasDatabaseName("ix_commerce_payment_transactions_intent_created");

        builder.HasIndex(transaction => new
            {
                transaction.TenantId,
                transaction.PaymentIntentId,
                transaction.ExternalEventId
            })
            .IsUnique()
            .HasFilter("external_event_id IS NOT NULL")
            .HasDatabaseName("ux_commerce_payment_transactions_external_event");
    }
}
