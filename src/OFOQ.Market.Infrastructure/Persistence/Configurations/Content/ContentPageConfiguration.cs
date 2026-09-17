using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Content;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Content;

public sealed class ContentPageConfiguration : IEntityTypeConfiguration<ContentPage>
{
    public void Configure(EntityTypeBuilder<ContentPage> b)
    {
        b.ToTable("content_pages");
        b.HasKey(x=>x.Id);
        b.Property(x=>x.Id).HasColumnName("id").HasConversion(x=>x.Value,x=>ContentPageId.From(x)).ValueGeneratedNever();
        b.Property(x=>x.TenantId).HasColumnName("tenant_id").HasConversion(x=>x.Value,x=>TenantId.From(x)).IsRequired();
        b.Property(x=>x.Title).HasColumnName("title").HasMaxLength(ContentPage.MaxTitleLength).IsRequired();
        b.Property(x=>x.Slug).HasColumnName("slug").HasMaxLength(ContentPage.MaxSlugLength).IsRequired();
        b.Property(x=>x.Body).HasColumnName("body").HasMaxLength(ContentPage.MaxBodyLength).IsRequired();
        b.Property(x=>x.SeoTitle).HasColumnName("seo_title").HasMaxLength(ContentPage.MaxSeoTitleLength);
        b.Property(x=>x.SeoDescription).HasColumnName("seo_description").HasMaxLength(ContentPage.MaxSeoDescriptionLength);
        b.Property(x=>x.IsPublished).HasColumnName("is_published").HasDefaultValue(false).IsRequired();
        b.Property(x=>x.PublishedAtUtc).HasColumnName("published_at_utc");
        b.Property(x=>x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired(); b.Property(x=>x.CreatedByUserId).HasColumnName("created_by_user_id"); b.Property(x=>x.UpdatedAtUtc).HasColumnName("updated_at_utc"); b.Property(x=>x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        b.HasIndex(x=>new{x.TenantId,x.Slug}).IsUnique().HasDatabaseName("ux_content_pages_tenant_slug");
        b.HasOne<Tenant>().WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
