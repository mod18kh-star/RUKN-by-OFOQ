using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Platform;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Platform;

public sealed class PlatformRequestConfiguration :
    IEntityTypeConfiguration<PlatformRequest>
{
    public void Configure(
        EntityTypeBuilder<PlatformRequest> builder)
    {
        builder.ToTable("platform_requests");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => PlatformRequestId.From(value))
            .ValueGeneratedNever();

        builder.Property(item => item.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.Property(item => item.RequestedByUserId)
            .HasColumnName("requested_by_user_id")
            .HasConversion(
                id => id.Value,
                value => UserId.From(value))
            .IsRequired();

        builder.Property(item => item.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.Summary)
            .HasColumnName("summary")
            .HasMaxLength(240)
            .IsRequired();

        builder.Property(item => item.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(item => item.RequestedAtUtc)
            .HasColumnName("requested_at_utc")
            .IsRequired();

        builder.Property(item => item.ReviewedAtUtc)
            .HasColumnName("reviewed_at_utc");

        builder.Property(item => item.ReviewedByUserId)
            .HasColumnName("reviewed_by_user_id")
            .HasConversion(
                id => id.HasValue
                    ? id.Value.Value
                    : (Guid?)null,
                value => value.HasValue
                    ? UserId.From(value.Value)
                    : (UserId?)null);

        builder.Property(item => item.ReviewReason)
            .HasColumnName("review_reason")
            .HasMaxLength(1000);

        builder.HasIndex(
                item => new
                {
                    item.TenantId,
                    item.Status,
                    item.RequestedAtUtc
                })
            .HasDatabaseName("ix_platform_requests_tenant_status_requested");

        builder.HasIndex(
                item => new
                {
                    item.Status,
                    item.Type,
                    item.RequestedAtUtc
                })
            .HasDatabaseName("ix_platform_requests_status_type_requested");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_requests_tenants_tenant_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_requests_users_requested_by_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_platform_requests_users_reviewed_by_user_id");
    }
}
