using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public sealed record ReviewEligibilityResult(bool IsEligible,OrderId? OrderId);
public sealed record PublicReviewResult(Guid Id,int Rating,string? Body,bool IsVerifiedPurchase,string? MerchantReply,DateTimeOffset CreatedAtUtc);
public sealed record PublicReviewPageResult(decimal AverageRating,int ReviewCount,IReadOnlyList<PublicReviewResult> Reviews);
public sealed record PublicStoreReviewResult(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    int Rating,
    string? Body,
    bool IsVerifiedPurchase,
    string? MerchantReply,
    DateTimeOffset CreatedAtUtc);
public sealed record PublicStoreReviewPageResult(decimal AverageRating,int ReviewCount,IReadOnlyList<PublicStoreReviewResult> Reviews);
public sealed record TrustMetricResult(long? CustomerCount,long? CompletedOrderCount,decimal? AverageRating,int? ReviewCount);

public interface IReviewQueryRepository
{
    Task<ReviewEligibilityResult> GetEligibilityAsync(UserId customerUserId,ProductId productId,CancellationToken cancellationToken=default);
    Task<PublicReviewPageResult?> GetPublicReviewsAsync(string storeSlug,string productSlug,int take,CancellationToken cancellationToken=default);
    Task<PublicStoreReviewPageResult?> GetPublicStoreReviewsAsync(string storeSlug,int take,CancellationToken cancellationToken=default);
    Task<TrustMetricResult?> GetTrustMetricsAsync(string storeSlug,CancellationToken cancellationToken=default);
}
