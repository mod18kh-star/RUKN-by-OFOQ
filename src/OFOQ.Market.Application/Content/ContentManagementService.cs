using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Content;

namespace OFOQ.Market.Application.Content;

public sealed record ContentPageResult(Guid Id,string Title,string Slug,string Body,string? SeoTitle,string? SeoDescription,bool IsPublished,DateTimeOffset? PublishedAtUtc,DateTimeOffset CreatedAtUtc);
public sealed record NavigationItemResult(Guid Id,string Location,string Type,string Label,Guid? TargetId,string? ExternalUrl,Guid? ParentItemId,int SortOrder,bool IsVisible);

public sealed class ContentManagementService
{
    private readonly ICurrentTenant _tenant;
    private readonly IContentPageRepository _pages;
    private readonly INavigationItemRepository _navigation;
    private readonly ICategoryRepository _categories;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;
    public ContentManagementService(ICurrentTenant tenant,IContentPageRepository pages,INavigationItemRepository navigation,ICategoryRepository categories,IProductRepository products,IUnitOfWork uow,TimeProvider time){_tenant=tenant;_pages=pages;_navigation=navigation;_categories=categories;_products=products;_uow=uow;_time=time;}

    public async Task<IReadOnlyList<ContentPageResult>> GetPagesAsync(CancellationToken ct=default){EnsureTenant();return (await _pages.GetAllAsync(ct)).OrderBy(x=>x.Title).Select(Map).ToArray();}
    public async Task<ContentPageResult> CreatePageAsync(string title,string slug,string body,string? seoTitle,string? seoDescription,bool publish,Guid actor,CancellationToken ct=default)
    { EnsureTenant(); if(await _pages.SlugExistsAsync(slug.Trim().ToLowerInvariant(),cancellationToken:ct))throw new ArgumentException("Page slug already exists."); var now=_time.GetUtcNow();var p=ContentPage.Create(_tenant.TenantId!.Value,title,slug,body,seoTitle,seoDescription,now,actor);if(publish)p.Publish(now,actor);await _pages.AddAsync(p,ct);await _uow.SaveChangesAsync(ct);return Map(p); }
    public async Task<ContentPageResult?> UpdatePageAsync(ContentPageId id,string title,string slug,string body,string? seoTitle,string? seoDescription,bool publish,Guid actor,CancellationToken ct=default)
    { EnsureTenant();var p=await _pages.GetByIdAsync(id,ct);if(p is null)return null;if(await _pages.SlugExistsAsync(slug.Trim().ToLowerInvariant(),id,ct))throw new ArgumentException("Page slug already exists.");var now=_time.GetUtcNow();p.Update(title,slug,body,seoTitle,seoDescription,now,actor);if(publish)p.Publish(now,actor);else p.Unpublish(now,actor);await _uow.SaveChangesAsync(ct);return Map(p); }
    public async Task<bool> DeletePageAsync(ContentPageId id,CancellationToken ct=default){EnsureTenant();var p=await _pages.GetByIdAsync(id,ct);if(p is null)return false;var nav=(await _navigation.GetAllAsync(ct)).Any(x=>x.TargetType==NavigationTargetType.Page&&x.TargetId==id.Value);if(nav)throw new InvalidOperationException("Remove page navigation links before deleting the page.");_pages.Remove(p);await _uow.SaveChangesAsync(ct);return true;}

    public async Task<IReadOnlyList<NavigationItemResult>> GetNavigationAsync(CancellationToken ct=default){EnsureTenant();return (await _navigation.GetAllAsync(ct)).OrderBy(x=>x.Location).ThenBy(x=>x.ParentItemId.HasValue).ThenBy(x=>x.SortOrder).Select(Map).ToArray();}
    public async Task<NavigationItemResult> CreateNavigationAsync(NavigationLocation location,NavigationTargetType type,string label,Guid? targetId,string? url,Guid? parentId,int? position,bool visible,Guid actor,CancellationToken ct=default)
    { EnsureTenant();NavigationItemId? parent=parentId.HasValue?NavigationItemId.From(parentId.Value):null;await ValidateTargetAsync(type,targetId,ct);await ValidateParentAsync(location,parent,null,ct);var siblings=(await _navigation.GetSiblingsAsync(location,parent,ct)).OrderBy(x=>x.SortOrder).ToList();var index=NormalizePosition(position,siblings.Count);var now=_time.GetUtcNow();for(var i=index;i<siblings.Count;i++)siblings[i].SetSortOrder(i+1,now,actor);var item=NavigationItem.Create(_tenant.TenantId!.Value,location,type,label,targetId,url,parent,index,visible,now,actor);await _navigation.AddAsync(item,ct);await _uow.SaveChangesAsync(ct);return Map(item); }
    public async Task<NavigationItemResult?> UpdateNavigationAsync(NavigationItemId id,NavigationLocation location,NavigationTargetType type,string label,Guid? targetId,string? url,Guid? parentId,int? position,bool visible,Guid actor,CancellationToken ct=default)
    { EnsureTenant();var item=await _navigation.GetByIdAsync(id,ct);if(item is null)return null;NavigationItemId? parent=parentId.HasValue?NavigationItemId.From(parentId.Value):null;await ValidateTargetAsync(type,targetId,ct);await ValidateParentAsync(location,parent,id,ct);var now=_time.GetUtcNow();var oldSiblings=(await _navigation.GetSiblingsAsync(item.Location,item.ParentItemId,ct)).Where(x=>x.Id!=id).OrderBy(x=>x.SortOrder).ToList();for(int i=0;i<oldSiblings.Count;i++)oldSiblings[i].SetSortOrder(i,now,actor);var newSiblings=(await _navigation.GetSiblingsAsync(location,parent,ct)).Where(x=>x.Id!=id).OrderBy(x=>x.SortOrder).ToList();var index=NormalizePosition(position,newSiblings.Count);for(int i=index;i<newSiblings.Count;i++)newSiblings[i].SetSortOrder(i+1,now,actor);item.Update(location,type,label,targetId,url,parent,index,visible,now,actor);await _uow.SaveChangesAsync(ct);return Map(item); }
    public async Task<bool> DeleteNavigationAsync(NavigationItemId id,CancellationToken ct=default){EnsureTenant();var item=await _navigation.GetByIdAsync(id,ct);if(item is null)return false;if((await _navigation.GetAllAsync(ct)).Any(x=>x.ParentItemId==id))throw new InvalidOperationException("Delete child navigation items first.");_navigation.Remove(item);await _uow.SaveChangesAsync(ct);return true;}

    private async Task ValidateTargetAsync(NavigationTargetType type,Guid? id,CancellationToken ct){if(type==NavigationTargetType.External)return;if(!id.HasValue||id.Value==Guid.Empty)throw new ArgumentException("Internal navigation target ID is required.");if(type==NavigationTargetType.Page&&await _pages.GetByIdAsync(ContentPageId.From(id.Value),ct) is null)throw new ArgumentException("Navigation page target was not found.");if(type==NavigationTargetType.Category&&await _categories.GetByIdAsync(CategoryId.From(id.Value),ct) is null)throw new ArgumentException("Navigation category target was not found.");if(type==NavigationTargetType.Product&&await _products.GetByIdAsync(ProductId.From(id.Value),ct) is null)throw new ArgumentException("Navigation product target was not found.");}
    private async Task ValidateParentAsync(NavigationLocation location,NavigationItemId? parent,NavigationItemId? current,CancellationToken ct){if(!parent.HasValue)return;if(current.HasValue&&parent.Value==current.Value)throw new ArgumentException("Navigation item cannot be its own parent.");var p=await _navigation.GetByIdAsync(parent.Value,ct)??throw new ArgumentException("Navigation parent was not found.");if(p.Location!=location)throw new ArgumentException("Navigation parent must use the same location.");var seen=new HashSet<NavigationItemId>();while(p.ParentItemId.HasValue){if(!seen.Add(p.Id))throw new InvalidOperationException("Navigation hierarchy contains a cycle.");if(current.HasValue&&p.ParentItemId.Value==current.Value)throw new ArgumentException("Navigation hierarchy cannot contain a cycle.");var next=await _navigation.GetByIdAsync(p.ParentItemId.Value,ct);if(next is null)break;p=next;}}
    private static int NormalizePosition(int? position,int count){if(!position.HasValue)return count;return Math.Clamp(position.Value-1,0,count);}
    private void EnsureTenant(){if(!_tenant.IsAvailable||!_tenant.TenantId.HasValue)throw new TenantScopeViolationException("Tenant context is required.");}
    private static ContentPageResult Map(ContentPage x)=>new(x.Id.Value,x.Title,x.Slug,x.Body,x.SeoTitle,x.SeoDescription,x.IsPublished,x.PublishedAtUtc,x.CreatedAtUtc);
    private static NavigationItemResult Map(NavigationItem x)=>new(x.Id.Value,x.Location.ToString(),x.TargetType.ToString(),x.Label,x.TargetId,x.ExternalUrl,x.ParentItemId?.Value,x.SortOrder,x.IsVisible);
}
