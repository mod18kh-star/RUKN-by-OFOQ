using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Reviews;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Reviews;

public sealed record ProductReviewResult(Guid Id,Guid ProductId,Guid CustomerUserId,Guid OrderId,int Rating,string? Body,bool IsVerifiedPurchase,string Status,string? MerchantReply,DateTimeOffset CreatedAtUtc,DateTimeOffset? PublishedAtUtc);
public sealed record TrustMetricSettingsResult(bool ShowCustomerCount,bool ShowCompletedOrderCount,bool ShowAverageRating);

public sealed class ReviewManagementService
{
 private readonly ICurrentTenant _tenant; private readonly IProductReviewRepository _reviews; private readonly IReviewQueryRepository _query; private readonly ITenantTrustMetricSettingsRepository _settings; private readonly IUnitOfWork _uow; private readonly TimeProvider _time;
 public ReviewManagementService(ICurrentTenant tenant,IProductReviewRepository reviews,IReviewQueryRepository query,ITenantTrustMetricSettingsRepository settings,IUnitOfWork uow,TimeProvider time){_tenant=tenant;_reviews=reviews;_query=query;_settings=settings;_uow=uow;_time=time;}
 public async Task<ProductReviewResult> SubmitAsync(UserId customer,ProductId product,int rating,string? body,CancellationToken ct=default){EnsureTenant();if(await _reviews.GetByCustomerAndProductAsync(customer,product,ct) is not null)throw new InvalidOperationException("Customer already reviewed this product.");var eligibility=await _query.GetEligibilityAsync(customer,product,ct);if(!eligibility.IsEligible||!eligibility.OrderId.HasValue)throw new InvalidOperationException("Only a delivered verified purchase can be reviewed.");var r=ProductReview.Create(_tenant.TenantId!.Value,product,customer,eligibility.OrderId.Value,rating,body,true,_time.GetUtcNow(),customer.Value);await _reviews.AddAsync(r,ct);await _uow.SaveChangesAsync(ct);return Map(r);}
 public async Task<IReadOnlyList<ProductReviewResult>> GetAllAsync(int take,CancellationToken ct=default){EnsureTenant();if(take<1||take>200)throw new ArgumentOutOfRangeException(nameof(take));return (await _reviews.GetAllAsync(take,ct)).Select(Map).ToArray();}
 public Task<ProductReviewResult?> PublishAsync(ProductReviewId id,Guid actor,CancellationToken ct=default)=>ChangeAsync(id,(r,n)=>r.Publish(n,actor),ct);
 public Task<ProductReviewResult?> HideAsync(ProductReviewId id,Guid actor,CancellationToken ct=default)=>ChangeAsync(id,(r,n)=>r.Hide(n,actor),ct);
 public Task<ProductReviewResult?> ReplyAsync(ProductReviewId id,string? reply,Guid actor,CancellationToken ct=default)=>ChangeAsync(id,(r,n)=>r.Reply(reply,n,actor),ct);
 public async Task<TrustMetricSettingsResult> GetSettingsAsync(CancellationToken ct=default){EnsureTenant();var s=await GetOrCreateSettingsAsync(ct);return Map(s);}
 public async Task<TrustMetricSettingsResult> UpdateSettingsAsync(bool customers,bool orders,bool rating,Guid actor,CancellationToken ct=default){EnsureTenant();var s=await GetOrCreateSettingsAsync(ct);s.Update(customers,orders,rating,_time.GetUtcNow(),actor);await _uow.SaveChangesAsync(ct);return Map(s);}
 private async Task<ProductReviewResult?> ChangeAsync(ProductReviewId id,Action<ProductReview,DateTimeOffset> mutate,CancellationToken ct){EnsureTenant();var r=await _reviews.GetByIdAsync(id,ct);if(r is null)return null;mutate(r,_time.GetUtcNow());await _uow.SaveChangesAsync(ct);return Map(r);}
 private async Task<TenantTrustMetricSettings> GetOrCreateSettingsAsync(CancellationToken ct){var s=await _settings.GetAsync(ct);if(s is not null)return s;s=TenantTrustMetricSettings.Create(_tenant.TenantId!.Value,_time.GetUtcNow());await _settings.AddAsync(s,ct);await _uow.SaveChangesAsync(ct);return s;}
 private void EnsureTenant(){if(!_tenant.IsAvailable||!_tenant.TenantId.HasValue)throw new TenantScopeViolationException("Tenant context is required.");}
 private static ProductReviewResult Map(ProductReview x)=>new(x.Id.Value,x.ProductId.Value,x.CustomerUserId.Value,x.OrderId.Value,x.Rating,x.Body,x.IsVerifiedPurchase,x.Status.ToString(),x.MerchantReply,x.CreatedAtUtc,x.PublishedAtUtc); private static TrustMetricSettingsResult Map(TenantTrustMetricSettings x)=>new(x.ShowCustomerCount,x.ShowCompletedOrderCount,x.ShowAverageRating);
}
