using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class TenantPaymentProviderAccountConfiguration :
    IEntityTypeConfiguration<TenantPaymentProviderAccount>
{
    public void Configure(
        EntityTypeBuilder<TenantPaymentProviderAccount> builder)
    {
        builder.ToTable(
            "commerce_tenant_payment_provider_accounts");

        builder.HasKey(
            account => account.Id);

        builder.HasAlternateKey(
                account => new
                {
                    account.TenantId,
                    account.Id
                })
            .HasName(
                "ak_commerce_tenant_payment_provider_accounts_tenant_id_id");

        builder.Property(
                account => account.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value =>
                    TenantPaymentProviderAccountId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                account => account.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value =>
                    TenantId.From(
                        value))
            .IsRequired();

        builder.Ignore(
            account => account.ProviderCode);

        builder.Ignore(
            account => account.HasCredentials);

        builder.Property<string>(
                "_providerCode")
            .HasColumnName("provider_code")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(
                account => account.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(
                account => account.Environment)
            .HasColumnName("environment")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                account => account.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.Property(
                account => account.ProtectedCredentials)
            .HasColumnName("protected_credentials")
            .HasColumnType("text");

        builder.Property(
                account => account.CredentialsVersion)
            .HasColumnName("credentials_version")
            .IsRequired();

        builder.Property(
                account => account.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(
                account => account.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(
                account => account.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(
                account => account.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.HasIndex(
                nameof(TenantPaymentProviderAccount.TenantId),
                "_providerCode",
                nameof(TenantPaymentProviderAccount.Environment))
            .IsUnique()
            .HasDatabaseName(
                "ux_commerce_pay_provider_accounts_tenant_provider_env");

        builder.HasIndex(
                nameof(TenantPaymentProviderAccount.TenantId),
                "_providerCode")
            .IsUnique()
            .HasFilter(
                "is_enabled = TRUE")
            .HasDatabaseName(
                "ux_commerce_pay_provider_accounts_tenant_provider_enabled");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                account => account.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_tenant_payment_provider_accounts_tenants");
    }
}