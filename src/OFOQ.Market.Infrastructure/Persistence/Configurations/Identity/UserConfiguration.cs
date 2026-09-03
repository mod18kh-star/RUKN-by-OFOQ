using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserConfiguration :
    IEntityTypeConfiguration<User>
{
    public void Configure(
        EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => UserId.From(value))
            .ValueGeneratedNever();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasConversion(
                email => email.Value,
                value => EmailAddress.Create(value))
            .HasMaxLength(254)
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("ux_users_email");

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(user => user.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.EmailVerifiedAtUtc)
            .HasColumnName("email_verified_at_utc");

        builder.Property(user => user.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(user => user.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(user => user.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(user => user.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.Property(user => user.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(user => user.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

        builder.Property(user => user.DeletedByUserId)
            .HasColumnName("deleted_by_user_id");

        builder.Ignore(user => user.DomainEvents);

        builder.HasQueryFilter(
            user => !user.IsDeleted);
    }
}