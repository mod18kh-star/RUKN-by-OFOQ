using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Tenancy;

public sealed class TenantDomainConfiguration :
    IEntityTypeConfiguration<TenantDomain>
{
    public void Configure(
        EntityTypeBuilder<TenantDomain> builder)
    {
        builder.ToTable("tenant_domains");

        builder.HasKey(domain => domain.Id);

        builder.Property(domain => domain.Id)
            .HasColumnName("id")
            .HasConversion(
                domainId => domainId.Value,
                value => TenantDomainId.From(value))
            .ValueGeneratedNever();

        builder.Property(domain => domain.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                tenantId => tenantId.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.Property(domain => domain.Domain)
            .HasColumnName("domain")
            .HasMaxLength(253)
            .HasConversion(
                domain => domain.Value,
                value => DomainName.Create(value))
            .IsRequired();

        builder.HasIndex(domain => domain.Domain)
            .IsUnique()
            .HasDatabaseName("ux_tenant_domains_domain");

        builder.Property(domain => domain.IsPrimary)
            .HasColumnName("is_primary")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(domain => domain.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(domain => domain.VerifiedAtUtc)
            .HasColumnName("verified_at_utc");

        builder.Property(domain => domain.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(domain => domain.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(domain => domain.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(domain => domain.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.Property(domain => domain.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(domain => domain.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");

        builder.Property(domain => domain.DeletedByUserId)
            .HasColumnName("deleted_by_user_id");

        builder.HasIndex(domain => domain.TenantId)
            .HasDatabaseName("ix_tenant_domains_tenant_id");

        builder.HasIndex(domain => new
            {
                domain.TenantId,
                domain.IsPrimary
            })
            .IsUnique()
            .HasFilter("\"is_primary\" = TRUE AND \"is_deleted\" = FALSE")
            .HasDatabaseName(
                "ux_tenant_domains_one_primary_per_tenant");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(domain => domain.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_tenant_domains_tenants_tenant_id");

        builder.HasQueryFilter(
            domain => !domain.IsDeleted);
    }
}