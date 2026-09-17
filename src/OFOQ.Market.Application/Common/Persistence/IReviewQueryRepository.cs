using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
namespace OFOQ.Market.Application.Common.Persistence;
public sealed record ReviewEligibilityResult(bool IsEligible,OrderId? OrderId);
public sealed record PublicReviewResult(Guid Id,int Rating,string? Body,bool IsVerifiedPurchase,string? MerchantReply,DateTimeOffset CreatedAtUtc);
public sealed record PublicReviewPageResult(decimal AverageRating,int ReviewCount,IReadOnlyList<PublicReviewResult> Reviews);
public sealed record TrustMetricResult(long? CustomerCount,long? CompletedOrderCount,decimal? AverageRating,int? ReviewCount);
public interface IReviewQueryRepository
{
 Task<ReviewEligibilityResult> GetEligibilityAsync(UserId customerUserId,ProductId productId,CancellationToken cancellationToken=default);
 Task<PublicReviewPageResult?> GetPublicReviewsAsync(string storeSlug,string productSlug,int take,CancellationToken cancellationToken=default);
 Task<TrustMetricResult?> GetTrustMetricsAsync(string storeSlug,CancellationToken cancellationToken=default);
}
