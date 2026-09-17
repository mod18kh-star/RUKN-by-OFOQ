using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Content;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class StorefrontContentQueryRepository : IStorefrontContentQueryRepository
{
    private readonly MarketDbContext _db;
    public StorefrontContentQueryRepository(MarketDbContext db)=>_db=db;

    public async Task<StorefrontContentPageResult?> GetPublishedPageAsync(string storeSlug,string pageSlug,CancellationToken cancellationToken=default)
    {
        var slug=TenantSlug.Create(storeSlug);
        var tenant=await _db.Tenants.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x=>x.Slug==slug&&!x.IsDeleted&&x.Status==TenantStatus.Active,cancellationToken);
        if(tenant is null)return null;
        return await _db.ContentPages.IgnoreQueryFilters().AsNoTracking().Where(x=>x.TenantId==tenant.Id&&x.Slug==pageSlug.Trim().ToLowerInvariant()&&x.IsPublished).Select(x=>new StorefrontContentPageResult(x.Id.Value,x.Title,x.Slug,x.Body,x.SeoTitle,x.SeoDescription,x.PublishedAtUtc)).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StorefrontNavigationItemResult>?> GetNavigationAsync(string storeSlug,NavigationLocation location,CancellationToken cancellationToken=default)
    {
        var slug=TenantSlug.Create(storeSlug);
        var tenant=await _db.Tenants.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x=>x.Slug==slug&&!x.IsDeleted&&x.Status==TenantStatus.Active,cancellationToken);
        if(tenant is null)return null;
        return await _db.NavigationItems.IgnoreQueryFilters().AsNoTracking().Where(x=>x.TenantId==tenant.Id&&x.Location==location&&x.IsVisible).OrderBy(x=>x.ParentItemId.HasValue).ThenBy(x=>x.SortOrder).Select(x=>new StorefrontNavigationItemResult(x.Id.Value,x.Location.ToString(),x.TargetType.ToString(),x.Label,x.TargetId,x.ExternalUrl,x.ParentItemId.HasValue ? x.ParentItemId.Value.Value : (Guid?)null,x.SortOrder)).ToArrayAsync(cancellationToken);
    }
}
