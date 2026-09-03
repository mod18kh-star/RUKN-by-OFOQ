using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Identity;

internal sealed class UserMfaRecoveryCodeConfiguration :
    IEntityTypeConfiguration<UserMfaRecoveryCode>
{
    public void Configure(
        EntityTypeBuilder<UserMfaRecoveryCode> builder)
    {
        builder.ToTable(
            "user_mfa_recovery_codes");

        builder.HasKey(
            recoveryCode => recoveryCode.Id);

        builder.Property(
                recoveryCode => recoveryCode.Id)
            .HasConversion(
                id => id.Value,
                value =>
                    UserMfaRecoveryCodeId.From(value))
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(
                recoveryCode => recoveryCode.UserMfaId)
            .HasConversion(
                id => id.Value,
                value => UserMfaId.From(value))
            .HasColumnName("user_mfa_id")
            .IsRequired();

        builder.Property(
                recoveryCode => recoveryCode.CodeHash)
            .HasColumnName("code_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(
                recoveryCode => recoveryCode.UsedAtUtc)
            .HasColumnName("used_at_utc")
            .IsConcurrencyToken();

        builder.Property(
                recoveryCode => recoveryCode.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(
                recoveryCode => recoveryCode.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(
                recoveryCode => recoveryCode.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(
                recoveryCode => recoveryCode.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.Ignore(
            recoveryCode => recoveryCode.IsUsed);

        builder.HasIndex(
                recoveryCode => recoveryCode.UserMfaId)
            .HasDatabaseName(
                "ix_user_mfa_recovery_codes_user_mfa_id");

        builder.HasIndex(
                recoveryCode => new
                {
                    recoveryCode.UserMfaId,
                    recoveryCode.CodeHash
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_user_mfa_recovery_codes_mfa_hash");

        builder.HasOne<UserMfa>()
            .WithMany()
            .HasForeignKey(
                recoveryCode =>
                    recoveryCode.UserMfaId)
            .OnDelete(
                DeleteBehavior.Cascade)
            .HasConstraintName(
                "fk_user_mfa_recovery_codes_user_mfa_id");
    }
}