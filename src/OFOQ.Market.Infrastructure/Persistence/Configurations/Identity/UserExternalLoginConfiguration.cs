using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        builder.ToTable("user_external_logins");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => UserExternalLoginId.From(value))
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(255).IsRequired();
        builder.Property(x => x.EmailAtLink).HasColumnName("email_at_link").HasMaxLength(254).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(x => new { x.Provider, x.Subject })
            .IsUnique()
            .HasDatabaseName("ux_user_external_logins_provider_subject");

        builder.HasIndex(x => new { x.UserId, x.Provider })
            .IsUnique()
            .HasDatabaseName("ux_user_external_logins_user_provider");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_user_external_logins_users_user_id");
    }
}
