using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Reviews;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
namespace OFOQ.Market.Infrastructure.Persistence.Repositories;
internal sealed class ReviewQueryRepository:IReviewQueryRepository
{
 private readonly MarketDbContext _db; public ReviewQueryRepository(MarketDbContext db)=>_db=db;
 public async Task<ReviewEligibilityResult> GetEligibilityAsync(UserId customerUserId,ProductId productId,CancellationToken cancellationToken=default)
 {
   var orderId=await _db.Orders.Where(o=>EF.Property<Guid>(o,"_customerUserId")==customerUserId.Value&&(o.FulfillmentStatus==OrderFulfillmentStatus.Delivered||o.FulfillmentStatus==OrderFulfillmentStatus.Fulfilled)&&_db.OrderItems.Any(i=>i.OrderId==o.Id&&i.ProductId==productId)).OrderByDescending(o=>o.CreatedAtUtc).Select(o=>(OrderId?)o.Id).FirstOrDefaultAsync(cancellationToken);
   return new(orderId.HasValue,orderId);
 }
 public async Task<PublicReviewPageResult?> GetPublicReviewsAsync(string storeSlug,string productSlug,int take,CancellationToken cancellationToken=default)
 {
   if(take<1||take>100)throw new ArgumentOutOfRangeException(nameof(take)); var slug=TenantSlug.Create(storeSlug);
   var tenant=await _db.Tenants.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x=>x.Slug==slug&&!x.IsDeleted&&x.Status==TenantStatus.Active,cancellationToken); if(tenant is null)return null;
   var product=await _db.Products.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x=>x.TenantId==tenant.Id&&x.Slug==productSlug.Trim().ToLowerInvariant()&&!x.IsDeleted&&x.Status==ProductStatus.Published&&x.IsVisible,cancellationToken); if(product is null)return null;
   var baseQuery=_db.ProductReviews.IgnoreQueryFilters().AsNoTracking().Where(x=>x.TenantId==tenant.Id&&x.ProductId==product.Id&&x.Status==ProductReviewStatus.Published);
   var count=await baseQuery.CountAsync(cancellationToken); var avg=count==0?0m:await baseQuery.AverageAsync(x=>(decimal)x.Rating,cancellationToken);
   var reviews=await baseQuery.OrderByDescending(x=>x.CreatedAtUtc).Take(take).Select(x=>new PublicReviewResult(x.Id.Value,x.Rating,x.Body,x.IsVerifiedPurchase,x.MerchantReply,x.CreatedAtUtc)).ToArrayAsync(cancellationToken);
   return new(decimal.Round(avg,2),count,reviews);
 }
 public async Task<TrustMetricResult?> GetTrustMetricsAsync(string storeSlug,CancellationToken cancellationToken=default)
 {
   var slug=TenantSlug.Create(storeSlug); var tenant=await _db.Tenants.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x=>x.Slug==slug&&!x.IsDeleted&&x.Status==TenantStatus.Active,cancellationToken); if(tenant is null)return null;
   var settings=await _db.TenantTrustMetricSettings.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x=>x.TenantId==tenant.Id,cancellationToken);
   var showCustomers=settings?.ShowCustomerCount??true; var showOrders=settings?.ShowCompletedOrderCount??true; var showRating=settings?.ShowAverageRating??true;
   var completed=_db.Orders.IgnoreQueryFilters().AsNoTracking().Where(o=>o.TenantId==tenant.Id&&(o.FulfillmentStatus==OrderFulfillmentStatus.Delivered||o.FulfillmentStatus==OrderFulfillmentStatus.Fulfilled));
   long? orderCount=showOrders?await completed.LongCountAsync(cancellationToken):null;
   long? customerCount=showCustomers?await completed.Select(o=>EF.Property<Guid>(o,"_customerUserId")).Distinct().LongCountAsync(cancellationToken):null;
   var published=_db.ProductReviews.IgnoreQueryFilters().AsNoTracking().Where(r=>r.TenantId==tenant.Id&&r.Status==ProductReviewStatus.Published); var reviewCount=await published.CountAsync(cancellationToken); decimal? average=showRating?(reviewCount==0?0m:decimal.Round(await published.AverageAsync(r=>(decimal)r.Rating,cancellationToken),2)):null;
   return new(customerCount,orderCount,average,showRating?reviewCount:null);
 }
}
