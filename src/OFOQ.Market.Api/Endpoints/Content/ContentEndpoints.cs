using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Application.Content;
using OFOQ.Market.Contracts.Content;
using OFOQ.Market.Domain.Content;

namespace OFOQ.Market.Api.Endpoints.Content;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var pages=endpoints.MapGroup("/api/tenants/{tenantId:guid}/backoffice/pages").WithTags("Content Pages").RequireAuthorization(AuthorizationPolicies.TenantBackOffice);
        pages.MapGet("",GetPagesAsync); pages.MapPost("",CreatePageAsync); pages.MapPut("/{id:guid}",UpdatePageAsync); pages.MapDelete("/{id:guid}",DeletePageAsync);

        var nav=endpoints.MapGroup("/api/tenants/{tenantId:guid}/backoffice/navigation").WithTags("Navigation").RequireAuthorization(AuthorizationPolicies.TenantBackOffice);
        nav.MapGet("",GetNavigationAsync); nav.MapPost("",CreateNavigationAsync); nav.MapPut("/{id:guid}",UpdateNavigationAsync); nav.MapDelete("/{id:guid}",DeleteNavigationAsync);

        var storefront=endpoints.MapGroup("/api/storefront/{storeSlug}").WithTags("Storefront Content");
        storefront.MapGet("/pages/{pageSlug}",GetStorefrontPageAsync);
        storefront.MapGet("/navigation",GetStorefrontNavigationAsync);
        return endpoints;
    }

    private static async Task<IResult> GetPagesAsync(ContentManagementService service,CancellationToken ct)=>Results.Ok((await service.GetPagesAsync(ct)).Select(Map).ToArray());
    private static async Task<IResult> CreatePageAsync(UpsertContentPageRequest r,ContentManagementService s,HttpContext h,CancellationToken ct){var a=Actor(h);if(!a.HasValue)return Results.Unauthorized();try{return Results.Ok(Map(await s.CreatePageAsync(r.Title,r.Slug,r.Body,r.SeoTitle,r.SeoDescription,r.Publish,a.Value,ct)));}catch(Exception e) when(e is ArgumentException or InvalidOperationException){return Validation("content_page_invalid",e.Message);} }
    private static async Task<IResult> UpdatePageAsync(Guid id,UpsertContentPageRequest r,ContentManagementService s,HttpContext h,CancellationToken ct){var a=Actor(h);if(!a.HasValue)return Results.Unauthorized();try{var x=await s.UpdatePageAsync(ContentPageId.From(id),r.Title,r.Slug,r.Body,r.SeoTitle,r.SeoDescription,r.Publish,a.Value,ct);return x is null?Results.NotFound():Results.Ok(Map(x));}catch(Exception e) when(e is ArgumentException or InvalidOperationException){return Validation("content_page_invalid",e.Message);} }
    private static async Task<IResult> DeletePageAsync(Guid id,ContentManagementService s,CancellationToken ct){try{return await s.DeletePageAsync(ContentPageId.From(id),ct)?Results.NoContent():Results.NotFound();}catch(InvalidOperationException e){return Validation("content_page_in_use",e.Message);} }

    private static async Task<IResult> GetNavigationAsync(ContentManagementService s,CancellationToken ct)=>Results.Ok((await s.GetNavigationAsync(ct)).Select(Map).ToArray());
    private static async Task<IResult> CreateNavigationAsync(UpsertNavigationItemRequest r,ContentManagementService s,HttpContext h,CancellationToken ct){var a=Actor(h);if(!a.HasValue)return Results.Unauthorized();if(!TryEnums(r,out var loc,out var type,out var err))return err!;try{return Results.Ok(Map(await s.CreateNavigationAsync(loc,type,r.Label,r.TargetId,r.ExternalUrl,r.ParentItemId,r.Position,r.IsVisible,a.Value,ct)));}catch(Exception e) when(e is ArgumentException or InvalidOperationException){return Validation("navigation_invalid",e.Message);} }
    private static async Task<IResult> UpdateNavigationAsync(Guid id,UpsertNavigationItemRequest r,ContentManagementService s,HttpContext h,CancellationToken ct){var a=Actor(h);if(!a.HasValue)return Results.Unauthorized();if(!TryEnums(r,out var loc,out var type,out var err))return err!;try{var x=await s.UpdateNavigationAsync(NavigationItemId.From(id),loc,type,r.Label,r.TargetId,r.ExternalUrl,r.ParentItemId,r.Position,r.IsVisible,a.Value,ct);return x is null?Results.NotFound():Results.Ok(Map(x));}catch(Exception e) when(e is ArgumentException or InvalidOperationException){return Validation("navigation_invalid",e.Message);} }
    private static async Task<IResult> DeleteNavigationAsync(Guid id,ContentManagementService s,CancellationToken ct){try{return await s.DeleteNavigationAsync(NavigationItemId.From(id),ct)?Results.NoContent():Results.NotFound();}catch(InvalidOperationException e){return Validation("navigation_in_use",e.Message);} }

    private static async Task<IResult> GetStorefrontPageAsync(string storeSlug,string pageSlug,IStorefrontContentQueryRepository q,CancellationToken ct){try{var x=await q.GetPublishedPageAsync(storeSlug,pageSlug,ct);return x is null?Results.NotFound():Results.Ok(new StorefrontPageResponse(x.Id,x.Title,x.Slug,x.Body,x.SeoTitle,x.SeoDescription,x.PublishedAtUtc));}catch(ArgumentException e){return Validation("storefront_content_invalid",e.Message);} }
    private static async Task<IResult> GetStorefrontNavigationAsync(string storeSlug,string? location,IStorefrontContentQueryRepository q,CancellationToken ct){if(!Enum.TryParse<NavigationLocation>(location??"Header",true,out var loc)||!Enum.IsDefined(loc))return Validation("navigation_location_invalid","Unsupported navigation location.");try{var x=await q.GetNavigationAsync(storeSlug,loc,ct);return x is null?Results.NotFound():Results.Ok(x.Select(i=>new StorefrontNavigationResponse(i.Id,i.Location,i.Type,i.Label,i.TargetId,i.ExternalUrl,i.ParentItemId,i.SortOrder)).ToArray());}catch(ArgumentException e){return Validation("storefront_content_invalid",e.Message);} }

    private static bool TryEnums(UpsertNavigationItemRequest r,out NavigationLocation location,out NavigationTargetType type,out IResult? error){error=null;if(!Enum.TryParse(r.Location,true,out location)||!Enum.IsDefined(location)){type=default;error=Validation("navigation_location_invalid","Unsupported navigation location.");return false;}if(!Enum.TryParse(r.Type,true,out type)||!Enum.IsDefined(type)){error=Validation("navigation_type_invalid","Unsupported navigation target type.");return false;}return true;}
    private static Guid? Actor(HttpContext h)=>Guid.TryParse(h.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,out var id)&&id!=Guid.Empty?id:null;
    private static ContentPageResponse Map(ContentPageResult x)=>new(x.Id,x.Title,x.Slug,x.Body,x.SeoTitle,x.SeoDescription,x.IsPublished,x.PublishedAtUtc,x.CreatedAtUtc);
    private static NavigationItemResponse Map(NavigationItemResult x)=>new(x.Id,x.Location,x.Type,x.Label,x.TargetId,x.ExternalUrl,x.ParentItemId,x.SortOrder,x.IsVisible);
    private static IResult Validation(string code,string message)=>Results.BadRequest(new{code,message});
}
