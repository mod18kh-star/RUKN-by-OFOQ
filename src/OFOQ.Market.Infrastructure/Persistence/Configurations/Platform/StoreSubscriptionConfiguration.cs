using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Platform;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Platform;

public sealed class StoreSubscriptionConfiguration :
    IEntityTypeConfiguration<StoreSubscription>
{
    public void Configure(
        EntityTypeBuilder<StoreSubscription> builder)
    {
        builder.ToTable("store_subscriptions");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => StoreSubscriptionId.From(value))
            .ValueGeneratedNever();

        builder.Property(item => item.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.Property(item => item.PlanCode)
            .HasColumnName("plan_code")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.BillingCycle)
            .HasColumnName("billing_cycle")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.StartedAtUtc)
            .HasColumnName("started_at_utc")
            .IsRequired();

        builder.Property(item => item.EndsAtUtc)
            .HasColumnName("ends_at_utc");

        builder.Property(item => item.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(item => item.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(item => item.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(item => item.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.HasIndex(item => item.TenantId)
            .IsUnique()
            .HasDatabaseName("ux_store_subscriptions_tenant");

        builder.HasIndex(
                item => new
                {
                    item.PlanCode,
                    item.Status
                })
            .HasDatabaseName("ix_store_subscriptions_plan_status");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_store_subscriptions_tenants_tenant_id");
    }
}
