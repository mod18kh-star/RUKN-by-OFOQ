using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Tenancy;

public sealed class TenantMembershipConfiguration :
    IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(
        EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("tenant_memberships");

        builder.HasKey(membership => membership.Id);

        builder.Property(membership => membership.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => TenantMembershipId.From(value))
            .ValueGeneratedNever();

        builder.Property(membership => membership.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.Property(membership => membership.UserId)
            .HasColumnName("user_id")
            .HasConversion(
                id => id.Value,
                value => UserId.From(value))
            .IsRequired();

        builder.Property(membership => membership.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(membership => membership.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(membership => membership.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(membership => membership.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(membership => membership.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.Property(membership => membership.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(membership => membership.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

        builder.Property(membership => membership.DeletedByUserId)
            .HasColumnName("deleted_by_user_id");

        builder.HasIndex(
                membership => new
                {
                    membership.TenantId,
                    membership.UserId
                })
            .IsUnique()
            .HasDatabaseName(
                "ux_tenant_memberships_tenant_user");

        builder.HasIndex(membership => membership.UserId)
            .HasDatabaseName(
                "ix_tenant_memberships_user_id");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(membership => membership.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_tenant_memberships_tenants_tenant_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_tenant_memberships_users_user_id");

        builder.HasQueryFilter(
            membership => !membership.IsDeleted);
    }
}