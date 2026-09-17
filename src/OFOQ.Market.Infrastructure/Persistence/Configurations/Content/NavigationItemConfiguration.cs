using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Content;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Content;

public sealed class NavigationItemConfiguration : IEntityTypeConfiguration<NavigationItem>
{
    public void Configure(EntityTypeBuilder<NavigationItem> b)
    {
        b.ToTable("content_navigation_items",t=>t.HasCheckConstraint("ck_navigation_sort_order","sort_order >= 0"));
        b.HasKey(x=>x.Id);
        b.Property(x=>x.Id).HasColumnName("id").HasConversion(x=>x.Value,x=>NavigationItemId.From(x)).ValueGeneratedNever();
        b.Property(x=>x.TenantId).HasColumnName("tenant_id").HasConversion(x=>x.Value,x=>TenantId.From(x)).IsRequired();
        b.Property(x=>x.Location).HasColumnName("location").HasConversion<int>().IsRequired();
        b.Property(x=>x.TargetType).HasColumnName("target_type").HasConversion<int>().IsRequired();
        b.Property(x=>x.Label).HasColumnName("label").HasMaxLength(NavigationItem.MaxLabelLength).IsRequired();
        b.Property(x=>x.TargetId).HasColumnName("target_id");
        b.Property(x=>x.ExternalUrl).HasColumnName("external_url").HasMaxLength(NavigationItem.MaxUrlLength);
        b.Property(x=>x.ParentItemId).HasColumnName("parent_item_id").HasConversion(id=>id.HasValue?id.Value.Value:(Guid?)null,v=>v.HasValue?NavigationItemId.From(v.Value):null);
        b.Property(x=>x.SortOrder).HasColumnName("sort_order").IsRequired(); b.Property(x=>x.IsVisible).HasColumnName("is_visible").HasDefaultValue(true).IsRequired();
        b.Property(x=>x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired(); b.Property(x=>x.CreatedByUserId).HasColumnName("created_by_user_id"); b.Property(x=>x.UpdatedAtUtc).HasColumnName("updated_at_utc"); b.Property(x=>x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        b.HasIndex(x=>new{x.TenantId,x.Location,x.ParentItemId,x.SortOrder}).IsUnique().HasDatabaseName("ux_navigation_sibling_order");
        b.HasOne<Tenant>().WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
