using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Identity;

internal sealed class UserSessionConfiguration :
    IEntityTypeConfiguration<UserSession>
{
    public void Configure(
        EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable(
            "user_sessions");

        builder.HasKey(
            session => session.Id);

        builder.Property(
                session => session.Id)
            .HasConversion(
                id => id.Value,
                value => UserSessionId.From(value))
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(
                session => session.UserId)
            .HasConversion(
                id => id.Value,
                value => UserId.From(value))
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(
                session => session.RefreshTokenHash)
            .HasColumnName("refresh_token_hash")
            .HasMaxLength(128)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(
                session => session.AuthenticationLevel)
            .HasColumnName("authentication_level")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                session => session.AuthenticationMethod)
            .HasColumnName("authentication_method")
            .HasConversion<int>()
            .HasDefaultValue(UserSessionAuthenticationMethod.Password)
            .IsRequired();

        builder.Property(
                session => session.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();

        builder.Property(
                session => session.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(
                session => session.LastSeenAtUtc)
            .HasColumnName("last_seen_at_utc")
            .IsRequired();

        builder.Property(
                session => session.LastRotatedAtUtc)
            .HasColumnName("last_rotated_at_utc")
            .IsRequired();

        builder.Property(
                session => session.RevokedAtUtc)
            .HasColumnName("revoked_at_utc");

        builder.Property(
                session => session.RevocationReason)
            .HasColumnName("revocation_reason")
            .HasMaxLength(200);

        builder.Property(
                session => session.CreatedIpAddress)
            .HasColumnName("created_ip_address")
            .HasMaxLength(64);

        builder.Property(
                session => session.LastIpAddress)
            .HasColumnName("last_ip_address")
            .HasMaxLength(64);

        builder.Property(
                session => session.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(512);

        builder.Ignore(
            session => session.IsRevoked);

        builder.HasIndex(
                session => session.RefreshTokenHash)
            .IsUnique()
            .HasDatabaseName(
                "ux_user_sessions_refresh_token_hash");

        builder.HasIndex(
                session =>
                    new
                    {
                        session.UserId,
                        session.ExpiresAtUtc
                    })
            .HasDatabaseName(
                "ix_user_sessions_user_expires");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(
                session => session.UserId)
            .OnDelete(
                DeleteBehavior.Cascade)
            .HasConstraintName(
                "fk_user_sessions_users_user_id");
    }
}
