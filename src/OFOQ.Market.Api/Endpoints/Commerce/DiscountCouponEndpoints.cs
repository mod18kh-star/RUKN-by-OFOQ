using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Commerce.Discounts;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Discounts;
using OFOQ.Market.Domain.Commerce.Discounts;
namespace OFOQ.Market.Api.Endpoints.Commerce;
public static class DiscountCouponEndpoints
{
    public static IEndpointRouteBuilder MapDiscountCouponEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var g = endpoints.MapGroup("/api/tenants/{tenantId:guid}/backoffice/coupons").WithTags("Coupons").RequireAuthorization(AuthorizationPolicies.TenantBackOffice);
        g.MapGet("", GetAsync); g.MapPost("", CreateAsync); g.MapPut("/{id:guid}", UpdateAsync); return endpoints;
    }
    private static async Task<IResult> GetAsync(CouponAdministrationService service, CancellationToken ct) => Results.Ok((await service.GetAllAsync(ct)).Select(Map).ToArray());
    private static async Task<IResult> CreateAsync(UpsertDiscountCouponRequest r, CouponAdministrationService service, HttpContext http, CancellationToken ct)
    {
        var actor=Actor(http); if(!actor.HasValue)return Results.Unauthorized(); if(!TryEnums(r,out var type,out var scope,out var error))return error!;
        try { return Results.Ok(Map(await service.CreateAsync(r.Code,r.Name,type,r.Value,scope,r.Currency,r.IncludeDescendantCategories,r.MinimumOrderAmount,r.MaximumTotalUses,r.MaximumUsesPerCustomer,r.StartsAtUtc,r.EndsAtUtc,r.ProductIds??[],r.CategoryIds??[],actor.Value,ct))); } catch(TenantScopeViolationException){return Results.Forbid();} catch(Exception e) when(e is ArgumentException or InvalidOperationException){return Validation(e.Message);}
    }
    private static async Task<IResult> UpdateAsync(Guid id, UpsertDiscountCouponRequest r, CouponAdministrationService service, HttpContext http, CancellationToken ct)
    {
        var actor=Actor(http); if(!actor.HasValue)return Results.Unauthorized(); if(!TryEnums(r,out var type,out var scope,out var error))return error!;
        try { var x=await service.UpdateAsync(DiscountCouponId.From(id),r.Code,r.Name,type,r.Value,scope,r.Currency,r.IncludeDescendantCategories,r.MinimumOrderAmount,r.MaximumTotalUses,r.MaximumUsesPerCustomer,r.StartsAtUtc,r.EndsAtUtc,r.IsEnabled,r.ProductIds??[],r.CategoryIds??[],actor.Value,ct); return x is null?Results.NotFound():Results.Ok(Map(x)); } catch(TenantScopeViolationException){return Results.Forbid();} catch(Exception e) when(e is ArgumentException or InvalidOperationException){return Validation(e.Message);}
    }
    private static bool TryEnums(UpsertDiscountCouponRequest r,out DiscountCouponType type,out DiscountCouponScope scope,out IResult? error)
    { error=null; if(!Enum.TryParse(r.Type,true,out type)||!Enum.IsDefined(type)){scope=default;error=Validation("Unsupported coupon type.");return false;} if(!Enum.TryParse(r.Scope,true,out scope)||!Enum.IsDefined(scope)){error=Validation("Unsupported coupon scope.");return false;} return true; }
    private static Guid? Actor(HttpContext h)=>Guid.TryParse(h.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,out var id)&&id!=Guid.Empty?id:null;
    private static DiscountCouponResponse Map(DiscountCouponResult x)=>new(x.Id,x.Code,x.Name,x.Type,x.Value,x.Scope,x.Currency,x.IncludeDescendantCategories,x.MinimumOrderAmount,x.MaximumTotalUses,x.MaximumUsesPerCustomer,x.StartsAtUtc,x.EndsAtUtc,x.IsEnabled,x.ProductIds,x.CategoryIds);
    private static IResult Validation(string message)=>Results.BadRequest(new{code="coupon_invalid",message});
}
