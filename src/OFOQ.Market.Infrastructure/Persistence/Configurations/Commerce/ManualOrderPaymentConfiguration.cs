using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class ManualOrderPaymentConfiguration : IEntityTypeConfiguration<ManualOrderPayment>
{
    public void Configure(EntityTypeBuilder<ManualOrderPayment> b)
    {
        b.ToTable("commerce_manual_order_payments");
        b.HasKey(x => x.Id);
        b.HasAlternateKey(x => new { x.TenantId, x.Id });
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.TenantId).HasColumnName("tenant_id")
            .HasConversion(x => x.Value, v => TenantId.From(v)).IsRequired();
        b.Property(x => x.OrderId).HasColumnName("order_id")
            .HasConversion(x => x.Value, v => OrderId.From(v)).IsRequired();
        b.Property(x => x.CustomerUserId).HasColumnName("customer_user_id").IsRequired();
        b.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        b.Property(x => x.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(40).IsRequired();
        b.Property(x => x.AccountName).HasColumnName("account_name").HasMaxLength(120).IsRequired();
        b.Property(x => x.AccountKind).HasColumnName("account_kind").HasMaxLength(20).IsRequired();
        b.Property(x => x.ProtectedAccountSnapshot).HasColumnName("protected_account_snapshot").HasColumnType("text").IsRequired();
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 4).IsRequired();
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(24).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        b.HasIndex(x => new { x.TenantId, x.OrderId }).IsUnique()
            .HasDatabaseName("ux_manual_order_payment_tenant_order");
        b.HasIndex(x => new { x.TenantId, x.Status, x.UpdatedAtUtc });
        b.HasOne<Order>().WithMany().HasForeignKey(x => new { x.TenantId, x.OrderId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ManualPaymentReceiptConfiguration : IEntityTypeConfiguration<ManualPaymentReceipt>
{
    public void Configure(EntityTypeBuilder<ManualPaymentReceipt> b)
    {
        b.ToTable("commerce_manual_payment_receipts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.TenantId).HasColumnName("tenant_id")
            .HasConversion(x => x.Value, v => TenantId.From(v)).IsRequired();
        b.Property(x => x.OrderId).HasColumnName("order_id")
            .HasConversion(x => x.Value, v => OrderId.From(v)).IsRequired();
        b.Property(x => x.ManualPaymentId).HasColumnName("manual_payment_id").IsRequired();
        b.Property(x => x.SubmittedByUserId).HasColumnName("submitted_by_user_id").IsRequired();
        b.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(20).IsRequired();
        b.Property(x => x.Ciphertext).HasColumnName("ciphertext").HasColumnType("bytea").IsRequired();
        b.Property(x => x.Nonce).HasColumnName("nonce").HasColumnType("bytea").IsRequired();
        b.Property(x => x.AuthTag).HasColumnName("auth_tag").HasColumnType("bytea").IsRequired();
        b.Property(x => x.TransferReference).HasColumnName("transfer_reference").HasMaxLength(100);
        b.Property(x => x.ReviewStatus).HasColumnName("review_status").HasMaxLength(24).IsRequired();
        b.Property(x => x.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);
        b.Property(x => x.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        b.Property(x => x.ReviewedAtUtc).HasColumnName("reviewed_at_utc");
        b.Property(x => x.SubmittedAtUtc).HasColumnName("submitted_at_utc").IsRequired();
        b.HasIndex(x => new { x.TenantId, x.ManualPaymentId, x.SubmittedAtUtc });
        b.HasOne<ManualOrderPayment>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ManualPaymentId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
