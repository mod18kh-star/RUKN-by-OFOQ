using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Tenancy;

public sealed class TenantConfiguration :
    IEntityTypeConfiguration<Tenant>
{
    public void Configure(
        EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(tenant => tenant.Id);

        builder.Property(tenant => tenant.Id)
            .HasColumnName("id")
            .HasConversion(
                tenantId => tenantId.Value,
                value => TenantId.From(value))
            .ValueGeneratedNever();

        builder.Property(tenant => tenant.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(tenant => tenant.Slug)
            .HasColumnName("slug")
            .HasMaxLength(63)
            .HasConversion(
                slug => slug.Value,
                value => TenantSlug.Create(value))
            .IsRequired();

        builder.HasIndex(tenant => tenant.Slug)
            .IsUnique()
            .HasDatabaseName("ux_tenants_slug");

        builder.Property(tenant => tenant.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(tenant => tenant.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(tenant => tenant.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(tenant => tenant.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(tenant => tenant.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.Property(tenant => tenant.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(tenant => tenant.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

        builder.Property(tenant => tenant.DeletedByUserId)
            .HasColumnName("deleted_by_user_id");

        builder.Ignore(tenant => tenant.DomainEvents);

        builder.HasQueryFilter(
            tenant => !tenant.IsDeleted);
    }
}