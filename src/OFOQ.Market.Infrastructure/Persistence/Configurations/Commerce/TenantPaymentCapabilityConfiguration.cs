using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class TenantPaymentCapabilityConfiguration :
    IEntityTypeConfiguration<TenantPaymentCapability>
{
    public void Configure(EntityTypeBuilder<TenantPaymentCapability> builder)
    {
        builder.ToTable("commerce_tenant_payment_capabilities");

        builder.HasKey(capability => capability.Id);

        builder.Property(capability => capability.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => TenantPaymentCapabilityId.From(value))
            .ValueGeneratedNever();

        builder.Property(capability => capability.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.Property(capability => capability.ElectronicPaymentsStatus)
            .HasColumnName("electronic_payments_status")
            .HasConversion<int>()
            .IsRequired();

        builder.Ignore(capability => capability.ElectronicPaymentsAllowed);

        builder.Property(capability => capability.SuspensionReason)
            .HasColumnName("suspension_reason")
            .HasMaxLength(500);

        builder.Property(capability => capability.SuspendedAtUtc)
            .HasColumnName("suspended_at_utc");

        builder.Property(capability => capability.SuspendedByUserId)
            .HasColumnName("suspended_by_user_id");

        builder.Property(capability => capability.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(capability => capability.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(capability => capability.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(capability => capability.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.HasIndex(capability => capability.TenantId)
            .IsUnique()
            .HasDatabaseName("ux_commerce_tenant_payment_capabilities_tenant");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(capability => capability.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_commerce_tenant_payment_capabilities_tenants");
    }
}
