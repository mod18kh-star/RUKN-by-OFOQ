using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserTrustedDeviceConfiguration :
    IEntityTypeConfiguration<UserTrustedDevice>
{
    public void Configure(
        EntityTypeBuilder<UserTrustedDevice> builder)
    {
        builder.ToTable(
            "user_trusted_devices");

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
                    UserTrustedDeviceId.From(value))
            .ValueGeneratedNever();

        builder.Property(
                item =>
                    item.UserId)
            .HasColumnName("user_id")
            .HasConversion(
                id => id.Value,
                value =>
                    UserId.From(value))
            .IsRequired();

        builder.Property(
                item =>
                    item.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(
                item =>
                    item.UserAgentHash)
            .HasColumnName("user_agent_hash")
            .HasMaxLength(128);

        builder.Property(
                item =>
                    item.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();

        builder.Property(
                item =>
                    item.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(
                item =>
                    item.LastUsedAtUtc)
            .HasColumnName("last_used_at_utc")
            .IsRequired();

        builder.Property(
                item =>
                    item.CreatedIpAddress)
            .HasColumnName("created_ip_address")
            .HasMaxLength(64);

        builder.Property(
                item =>
                    item.LastIpAddress)
            .HasColumnName("last_ip_address")
            .HasMaxLength(64);

        builder.Property(
                item =>
                    item.RevokedAtUtc)
            .HasColumnName("revoked_at_utc");

        builder.Property(
                item =>
                    item.RevocationReason)
            .HasColumnName("revocation_reason")
            .HasMaxLength(200);

        builder.HasIndex(
                item =>
                    item.TokenHash)
            .IsUnique()
            .HasDatabaseName(
                "ux_user_trusted_devices_token_hash");

        builder.HasIndex(
                item =>
                    new
                    {
                        item.UserId,
                        item.RevokedAtUtc,
                        item.ExpiresAtUtc
                    })
            .HasDatabaseName(
                "ix_user_trusted_devices_user_active");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(
                item =>
                    item.UserId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
