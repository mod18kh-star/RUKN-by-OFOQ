using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Identity;

public sealed class PlatformUserRoleAssignmentConfiguration :
    IEntityTypeConfiguration<PlatformUserRoleAssignment>
{
    public void Configure(
        EntityTypeBuilder<PlatformUserRoleAssignment> builder)
    {
        builder.ToTable(
            "platform_user_role_assignments",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_platform_user_role_assignments_role",
                    "role IN ('PlatformAdministrator', 'ComplianceReviewer')");

                tableBuilder.HasCheckConstraint(
                    "ck_platform_user_role_assignments_deleted_audit",
                    "NOT is_deleted OR deleted_at_utc IS NOT NULL");
            });

        builder.HasKey(
            assignment => assignment.Id);

        builder.Property(
                assignment => assignment.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id => id.Value,
                value =>
                    PlatformUserRoleAssignmentId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                assignment => assignment.UserId)
            .HasColumnName(
                "user_id")
            .HasConversion(
                id => id.Value,
                value =>
                    UserId.From(
                        value))
            .IsRequired();

        builder.Property(
                assignment => assignment.Role)
            .HasColumnName(
                "role")
            .HasConversion<string>()
            .HasMaxLength(
                64)
            .IsRequired();

        builder.Property(
                assignment => assignment.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                assignment => assignment.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.Property(
                assignment => assignment.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(
                assignment => assignment.UpdatedByUserId)
            .HasColumnName(
                "updated_by_user_id");

        builder.Property(
                assignment => assignment.IsDeleted)
            .HasColumnName(
                "is_deleted")
            .HasDefaultValue(
                false)
            .IsRequired();

        builder.Property(
                assignment => assignment.DeletedAtUtc)
            .HasColumnName(
                "deleted_at_utc");

        builder.Property(
                assignment => assignment.DeletedByUserId)
            .HasColumnName(
                "deleted_by_user_id");

        builder.HasIndex(
                assignment => assignment.UserId)
            .HasDatabaseName(
                "ix_platform_user_role_assignments_user_id");

        builder.HasIndex(
                assignment => new
                {
                    assignment.UserId,
                    assignment.Role
                })
            .IsUnique()
            .HasFilter(
                "is_deleted = false")
            .HasDatabaseName(
                "ux_platform_user_role_assignments_user_role_active");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(
                assignment =>
                    assignment.UserId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_platform_user_role_assignments_users_user_id");

        builder.HasQueryFilter(
            assignment =>
                !assignment.IsDeleted);
    }
}
