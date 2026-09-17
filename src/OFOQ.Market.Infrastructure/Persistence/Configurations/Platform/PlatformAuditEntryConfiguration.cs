using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Platform;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Platform;

public sealed class PlatformAuditEntryConfiguration :
    IEntityTypeConfiguration<PlatformAuditEntry>
{
    public void Configure(
        EntityTypeBuilder<PlatformAuditEntry> builder)
    {
        builder.ToTable("platform_audit_entries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => PlatformAuditEntryId.From(value))
            .ValueGeneratedNever();

        builder.Property(entry => entry.ActorUserId)
            .HasColumnName("actor_user_id")
            .HasConversion(
                id => id.Value,
                value => UserId.From(value))
            .IsRequired();

        builder.Property(entry => entry.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.Property(entry => entry.Action)
            .HasColumnName("action")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(entry => entry.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(entry => entry.OldValueJson)
            .HasColumnName("old_value_json")
            .HasColumnType("jsonb");

        builder.Property(entry => entry.NewValueJson)
            .HasColumnName("new_value_json")
            .HasColumnType("jsonb");

        builder.Property(entry => entry.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(64);

        builder.Property(entry => entry.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(512);

        builder.Property(entry => entry.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.HasIndex(entry => new
            {
                entry.TenantId,
                entry.OccurredAtUtc
            })
            .HasDatabaseName("ix_platform_audit_entries_tenant_occurred");

        builder.HasIndex(entry => new
            {
                entry.ActorUserId,
                entry.OccurredAtUtc
            })
            .HasDatabaseName("ix_platform_audit_entries_actor_occurred");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entry => entry.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_audit_entries_users_actor_user_id");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(entry => entry.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_audit_entries_tenants_tenant_id");
    }
}
