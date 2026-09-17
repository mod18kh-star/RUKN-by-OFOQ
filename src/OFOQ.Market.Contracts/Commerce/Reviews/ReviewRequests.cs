namespace OFOQ.Market.Contracts.Commerce.Reviews;
public sealed record SubmitProductReviewRequest(Guid ProductId,int Rating,string? Body);
public sealed record ReviewReplyRequest(string? Reply);
public sealed record ProductReviewResponse(Guid Id,Guid ProductId,Guid CustomerUserId,Guid OrderId,int Rating,string? Body,bool IsVerifiedPurchase,string Status,string? MerchantReply,DateTimeOffset CreatedAtUtc,DateTimeOffset? PublishedAtUtc);
public sealed record TrustMetricSettingsRequest(bool ShowCustomerCount,bool ShowCompletedOrderCount,bool ShowAverageRating);
public sealed record TrustMetricSettingsResponse(bool ShowCustomerCount,bool ShowCompletedOrderCount,bool ShowAverageRating);
public sealed record PublicProductReviewResponse(Guid Id,int Rating,string? Body,bool IsVerifiedPurchase,string? MerchantReply,DateTimeOffset CreatedAtUtc);
public sealed record PublicProductReviewPageResponse(decimal AverageRating,int ReviewCount,IReadOnlyList<PublicProductReviewResponse> Reviews);
public sealed record PublicTrustMetricResponse(long? CustomerCount,long? CompletedOrderCount,decimal? AverageRating,int? ReviewCount);
