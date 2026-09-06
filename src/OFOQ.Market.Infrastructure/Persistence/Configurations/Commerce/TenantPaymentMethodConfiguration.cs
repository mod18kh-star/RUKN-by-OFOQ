using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class TenantPaymentMethodConfiguration :
    IEntityTypeConfiguration<TenantPaymentMethod>
{
    public void Configure(EntityTypeBuilder<TenantPaymentMethod> builder)
    {
        builder.ToTable(
            "commerce_tenant_payment_methods",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_commerce_tenant_payment_methods_minimum_nonnegative",
                    "minimum_amount IS NULL OR minimum_amount >= 0");
                table.HasCheckConstraint(
                    "ck_commerce_tenant_payment_methods_maximum_nonnegative",
                    "maximum_amount IS NULL OR maximum_amount >= 0");
                table.HasCheckConstraint(
                    "ck_commerce_tenant_payment_methods_limits_order",
                    "minimum_amount IS NULL OR maximum_amount IS NULL OR minimum_amount <= maximum_amount");
            });

        builder.HasKey(method => method.Id);

        builder.HasAlternateKey(method => new
            {
                method.TenantId,
                method.Id
            })
            .HasName("ak_commerce_tenant_payment_methods_tenant_id_id");

        builder.Property(method => method.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => TenantPaymentMethodId.From(value))
            .ValueGeneratedNever();

        builder.Property(method => method.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.Property(method => method.Type)
            .HasColumnName("method_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Ignore(method => method.ProviderCode);
        builder.Ignore(method => method.Country);
        builder.Ignore(method => method.Currency);

        builder.Property<string>("_providerCode")
            .HasColumnName("provider_code")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(method => method.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property<string>("_countryCode")
            .HasColumnName("country_code")
            .HasMaxLength(2)
            .IsRequired();

        builder.Property<string>("_currencyCode")
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(method => method.MinimumAmount)
            .HasColumnName("minimum_amount")
            .HasPrecision(18, 4);

        builder.Property(method => method.MaximumAmount)
            .HasColumnName("maximum_amount")
            .HasPrecision(18, 4);

        builder.Property(method => method.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.Property(method => method.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(method => method.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(method => method.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(method => method.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.HasIndex(
                nameof(TenantPaymentMethod.TenantId),
                "_providerCode",
                "_currencyCode")
            .HasDatabaseName("ix_commerce_tenant_payment_methods_provider_currency");

        builder.HasIndex(method => new
            {
                method.TenantId,
                method.IsEnabled
            })
            .HasDatabaseName("ix_commerce_tenant_payment_methods_tenant_enabled");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(method => method.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_commerce_tenant_payment_methods_tenants");
    }
}
