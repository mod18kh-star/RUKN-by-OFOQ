using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Identity;

internal sealed class MfaLoginChallengeConfiguration :
    IEntityTypeConfiguration<MfaLoginChallenge>
{
    public void Configure(
        EntityTypeBuilder<MfaLoginChallenge> builder)
    {
        builder.ToTable(
            "mfa_login_challenges");

        builder.HasKey(
            challenge => challenge.Id);

        builder.Property(
                challenge => challenge.Id)
            .HasConversion(
                id => id.Value,
                value =>
                    MfaLoginChallengeId.From(value))
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(
                challenge => challenge.UserId)
            .HasConversion(
                id => id.Value,
                value =>
                    UserId.From(value))
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(
                challenge => challenge.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(
                challenge => challenge.AuthenticationMethod)
            .HasColumnName("authentication_method")
            .HasConversion<int>()
            .HasDefaultValue(UserSessionAuthenticationMethod.Password)
            .IsRequired();

        builder.Property(
                challenge => challenge.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();

        builder.Property(
                challenge => challenge.ConsumedAtUtc)
            .HasColumnName("consumed_at_utc")
            .IsConcurrencyToken();

        builder.Property(
                challenge => challenge.RevokedAtUtc)
            .HasColumnName("revoked_at_utc")
            .IsConcurrencyToken();

        builder.Property(
                challenge => challenge.FailedAttemptCount)
            .HasColumnName("failed_attempt_count")
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(
                challenge => challenge.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(
                challenge => challenge.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(
                challenge => challenge.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(
                challenge => challenge.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.Ignore(
            challenge => challenge.IsConsumed);

        builder.Ignore(
            challenge => challenge.IsRevoked);

        builder.Ignore(
            challenge => challenge.IsExhausted);

        builder.HasIndex(
                challenge => challenge.TokenHash)
            .IsUnique()
            .HasDatabaseName(
                "ux_mfa_login_challenges_token_hash");

        builder.HasIndex(
                challenge => new
                {
                    challenge.UserId,
                    challenge.ExpiresAtUtc
                })
            .HasDatabaseName(
                "ix_mfa_login_challenges_user_expires");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(
                challenge =>
                    challenge.UserId)
            .OnDelete(
                DeleteBehavior.Cascade)
            .HasConstraintName(
                "fk_mfa_login_challenges_users_user_id");
    }
}