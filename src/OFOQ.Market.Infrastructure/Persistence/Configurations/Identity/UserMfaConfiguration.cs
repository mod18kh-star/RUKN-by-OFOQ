using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Identity;

internal sealed class UserMfaConfiguration :
    IEntityTypeConfiguration<UserMfa>
{
    public void Configure(
        EntityTypeBuilder<UserMfa> builder)
    {
        builder.ToTable(
            "user_mfa");

        builder.HasKey(
            userMfa => userMfa.Id);

        builder.Property(
                userMfa => userMfa.Id)
            .HasConversion(
                id => id.Value,
                value => UserMfaId.From(value))
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(
                userMfa => userMfa.UserId)
            .HasConversion(
                id => id.Value,
                value => UserId.From(value))
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(
                userMfa => userMfa.ProtectedSecret)
            .HasColumnName("protected_secret")
            .HasMaxLength(4096)
            .IsRequired();

        builder.Property(
                userMfa => userMfa.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(
                userMfa => userMfa.EnabledAtUtc)
            .HasColumnName("enabled_at_utc");

        builder.Property(
                userMfa => userMfa.LastAcceptedTimeStep)
            .HasColumnName("last_accepted_time_step")
            .IsConcurrencyToken();

        builder.Property(
                userMfa => userMfa.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(
                userMfa => userMfa.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(
                userMfa => userMfa.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(
                userMfa => userMfa.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.Ignore(
            userMfa => userMfa.DomainEvents);

        builder.HasIndex(
                userMfa => userMfa.UserId)
            .IsUnique()
            .HasDatabaseName(
                "ux_user_mfa_user_id");

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<UserMfa>(
                userMfa => userMfa.UserId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_user_mfa_users_user_id");
    }
}