using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class MerchantManualPaymentAccountConfiguration : IEntityTypeConfiguration<MerchantManualPaymentAccount>
{
    public void Configure(EntityTypeBuilder<MerchantManualPaymentAccount> builder)
    {
        builder.ToTable("commerce_merchant_manual_payment_accounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.TenantId).HasColumnName("tenant_id")
            .HasConversion(x => x.Value, x => TenantId.From(x)).IsRequired();
        builder.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(20).IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.MaskedReference).HasColumnName("masked_reference").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProtectedDetails).HasColumnName("protected_details").HasColumnType("text").IsRequired();
        builder.Property(x => x.HasQr).HasColumnName("has_qr").IsRequired();
        builder.Property(x => x.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id").IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.Id }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
    }
}
