using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class TenantPaymentWalletCapabilityConfiguration :
    IEntityTypeConfiguration<TenantPaymentWalletCapability>
{
    public void Configure(
        EntityTypeBuilder<TenantPaymentWalletCapability> builder)
    {
        builder.ToTable(
            "commerce_tenant_payment_wallet_capabilities");

        builder.HasKey(
            capability => capability.Id);

        builder.Property(
                capability => capability.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value =>
                    TenantPaymentWalletCapabilityId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                capability => capability.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value =>
                    TenantId.From(
                        value))
            .IsRequired();

        builder.Property(
                capability => capability.ProviderAccountId)
            .HasColumnName("provider_account_id")
            .HasConversion(
                id => id.Value,
                value =>
                    TenantPaymentProviderAccountId.From(
                        value))
            .IsRequired();

        builder.Property(
                capability => capability.WalletType)
            .HasColumnName("wallet_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                capability => capability.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.Property(
                capability => capability.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(
                capability => capability.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(
                capability => capability.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(
                capability => capability.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.HasIndex(
                capability => new
                {
                    capability.TenantId,
                    capability.ProviderAccountId,
                    capability.WalletType
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_commerce_pay_wallet_caps_tenant_account_wallet");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                capability => capability.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_tenant_payment_wallet_capabilities_tenants");

        builder.HasOne<TenantPaymentProviderAccount>()
            .WithMany()
            .HasForeignKey(
                capability => new
                {
                    capability.TenantId,
                    capability.ProviderAccountId
                })
            .HasPrincipalKey(
                account => new
                {
                    account.TenantId,
                    account.Id
                })
            .OnDelete(
                DeleteBehavior.Cascade)
            .HasConstraintName(
                "fk_commerce_pay_wallet_caps_provider_accounts");
    }
}