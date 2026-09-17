using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Commerce.Reviews;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Reviews;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Reviews;
using OFOQ.Market.Domain.Identity;
namespace OFOQ.Market.Api.Endpoints.Commerce;
public static class ReviewEndpoints
{
 public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder e)
 {
  var account=e.MapGroup("/api/tenants/{tenantId:guid}/account/reviews").WithTags("Customer Reviews").RequireAuthorization(); account.MapPost("",SubmitAsync);
  var back=e.MapGroup("/api/tenants/{tenantId:guid}/backoffice/reviews").WithTags("Back Office Reviews").RequireAuthorization(AuthorizationPolicies.TenantBackOffice); back.MapGet("",GetAllAsync);back.MapPost("/{id:guid}/publish",PublishAsync);back.MapPost("/{id:guid}/hide",HideAsync);back.MapPut("/{id:guid}/reply",ReplyAsync);back.MapGet("/trust-settings",GetSettingsAsync);back.MapPut("/trust-settings",UpdateSettingsAsync);
  var sf=e.MapGroup("/api/storefront/{storeSlug}").WithTags("Storefront Reviews"); sf.MapGet("/products/{productSlug}/reviews",PublicReviewsAsync);sf.MapGet("/trust-metrics",TrustMetricsAsync);return e;
 }
 private static async Task<IResult> SubmitAsync(SubmitProductReviewRequest r,ReviewManagementService s,HttpContext h,CancellationToken ct){var u=User(h);if(!u.HasValue)return Results.Unauthorized();try{return Results.Ok(Map(await s.SubmitAsync(UserId.From(u.Value),ProductId.From(r.ProductId),r.Rating,r.Body,ct)));}catch(TenantScopeViolationException){return Results.Forbid();}catch(Exception ex) when(ex is ArgumentException or InvalidOperationException){return Bad("review_invalid",ex.Message);} }
 private static async Task<IResult> GetAllAsync(int? take,ReviewManagementService s,CancellationToken ct){try{return Results.Ok((await s.GetAllAsync(take??100,ct)).Select(Map).ToArray());}catch(ArgumentException ex){return Bad("review_query_invalid",ex.Message);} }
 private static Task<IResult> PublishAsync(Guid id,ReviewManagementService s,HttpContext h,CancellationToken ct)=>Act(id,(x,a)=>s.PublishAsync(x,a,ct),s,h);
 private static Task<IResult> HideAsync(Guid id,ReviewManagementService s,HttpContext h,CancellationToken ct)=>Act(id,(x,a)=>s.HideAsync(x,a,ct),s,h);
 private static async Task<IResult> ReplyAsync(Guid id,ReviewReplyRequest r,ReviewManagementService s,HttpContext h,CancellationToken ct){var a=User(h);if(!a.HasValue)return Results.Unauthorized();try{var x=await s.ReplyAsync(ProductReviewId.From(id),r.Reply,a.Value,ct);return x is null?Results.NotFound():Results.Ok(Map(x));}catch(ArgumentException ex){return Bad("review_reply_invalid",ex.Message);} }
 private static async Task<IResult> Act(Guid id,Func<ProductReviewId,Guid,Task<ProductReviewResult?>> action,ReviewManagementService s,HttpContext h){var a=User(h);if(!a.HasValue)return Results.Unauthorized();var x=await action(ProductReviewId.From(id),a.Value);return x is null?Results.NotFound():Results.Ok(Map(x));}
 private static async Task<IResult> GetSettingsAsync(ReviewManagementService s,CancellationToken ct){var x=await s.GetSettingsAsync(ct);return Results.Ok(new TrustMetricSettingsResponse(x.ShowCustomerCount,x.ShowCompletedOrderCount,x.ShowAverageRating));}
 private static async Task<IResult> UpdateSettingsAsync(TrustMetricSettingsRequest r,ReviewManagementService s,HttpContext h,CancellationToken ct){var a=User(h);if(!a.HasValue)return Results.Unauthorized();var x=await s.UpdateSettingsAsync(r.ShowCustomerCount,r.ShowCompletedOrderCount,r.ShowAverageRating,a.Value,ct);return Results.Ok(new TrustMetricSettingsResponse(x.ShowCustomerCount,x.ShowCompletedOrderCount,x.ShowAverageRating));}
 private static async Task<IResult> PublicReviewsAsync(string storeSlug,string productSlug,int? take,IReviewQueryRepository q,CancellationToken ct){try{var x=await q.GetPublicReviewsAsync(storeSlug,productSlug,take??20,ct);return x is null?Results.NotFound():Results.Ok(new PublicProductReviewPageResponse(x.AverageRating,x.ReviewCount,x.Reviews.Select(r=>new PublicProductReviewResponse(r.Id,r.Rating,r.Body,r.IsVerifiedPurchase,r.MerchantReply,r.CreatedAtUtc)).ToArray()));}catch(ArgumentException ex){return Bad("review_query_invalid",ex.Message);} }
 private static async Task<IResult> TrustMetricsAsync(string storeSlug,IReviewQueryRepository q,CancellationToken ct){try{var x=await q.GetTrustMetricsAsync(storeSlug,ct);return x is null?Results.NotFound():Results.Ok(new PublicTrustMetricResponse(x.CustomerCount,x.CompletedOrderCount,x.AverageRating,x.ReviewCount));}catch(ArgumentException ex){return Bad("trust_metrics_invalid",ex.Message);} }
 private static Guid? User(HttpContext h)=>Guid.TryParse(h.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,out var id)&&id!=Guid.Empty?id:null; private static ProductReviewResponse Map(ProductReviewResult x)=>new(x.Id,x.ProductId,x.CustomerUserId,x.OrderId,x.Rating,x.Body,x.IsVerifiedPurchase,x.Status,x.MerchantReply,x.CreatedAtUtc,x.PublishedAtUtc); private static IResult Bad(string code,string message)=>Results.BadRequest(new{code,message});
}
