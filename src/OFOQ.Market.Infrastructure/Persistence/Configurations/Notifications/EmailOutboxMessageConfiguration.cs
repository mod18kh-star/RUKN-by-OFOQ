using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Notifications;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Notifications;

public sealed class EmailOutboxMessageConfiguration :
    IEntityTypeConfiguration<EmailOutboxMessage>
{
    public void Configure(
        EntityTypeBuilder<EmailOutboxMessage> builder)
    {
        builder.ToTable(
            "email_outbox_messages");

        builder.HasKey(
            item =>
                item.Id);

        builder.Property(
                item =>
                    item.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value =>
                    EmailOutboxMessageId.From(value))
            .ValueGeneratedNever();

        builder.Property(
                item =>
                    item.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(
                item =>
                    item.ToEmail)
            .HasColumnName("to_email")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(
                item =>
                    item.Subject)
            .HasColumnName("subject")
            .HasMaxLength(240)
            .IsRequired();

        builder.Property(
                item =>
                    item.TextBody)
            .HasColumnName("text_body")
            .HasMaxLength(12000)
            .IsRequired();

        builder.Property(
                item =>
                    item.HtmlBody)
            .HasColumnName("html_body")
            .HasMaxLength(24000);

        builder.Property(
                item =>
                    item.Kind)
            .HasColumnName("kind")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(
                item =>
                    item.DedupeKey)
            .HasColumnName("dedupe_key")
            .HasMaxLength(220)
            .IsRequired();

        builder.Property(
                item =>
                    item.State)
            .HasColumnName("state")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                item =>
                    item.Attempts)
            .HasColumnName("attempts")
            .IsRequired();

        builder.Property(
                item =>
                    item.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc")
            .IsRequired();

        builder.Property(
                item =>
                    item.LeaseExpiresAtUtc)
            .HasColumnName("lease_expires_at_utc");

        builder.Property(
                item =>
                    item.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(
                item =>
                    item.SentAtUtc)
            .HasColumnName("sent_at_utc");

        builder.Property(
                item =>
                    item.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(2000);

        builder.HasIndex(
                item =>
                    item.DedupeKey)
            .IsUnique()
            .HasDatabaseName(
                "ux_email_outbox_dedupe");

        builder.HasIndex(
                item =>
                    new
                    {
                        item.State,
                        item.NextAttemptAtUtc
                    })
            .HasDatabaseName(
                "ix_email_outbox_due");
    }
}
