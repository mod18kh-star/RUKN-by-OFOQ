using System.Text;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
namespace OFOQ.Market.Domain.Commerce.Reviews;

public sealed class ProductReview:Entity<ProductReviewId>,ITenantDataScoped,IAuditable
{
 public const int MaxBodyLength=4000; public const int MaxReplyLength=2000; private ProductReview(){}
 private ProductReview(ProductReviewId id,TenantId tenantId,ProductId productId,UserId customerUserId,OrderId orderId,int rating,string? body,bool verified,DateTimeOffset at,Guid? by):base(id){if(tenantId.IsEmpty||productId.IsEmpty||customerUserId.IsEmpty||orderId.IsEmpty)throw new ArgumentException("Review identity values cannot be empty.");TenantId=tenantId;ProductId=productId;CustomerUserId=customerUserId;OrderId=orderId;Rating=ValidateRating(rating);Body=Norm(body,MaxBodyLength);IsVerifiedPurchase=verified;Status=ProductReviewStatus.Pending;CreatedAtUtc=at;CreatedByUserId=by;}
 public TenantId TenantId{get;private set;} public ProductId ProductId{get;private set;} public UserId CustomerUserId{get;private set;} public OrderId OrderId{get;private set;} public int Rating{get;private set;} public string? Body{get;private set;} public bool IsVerifiedPurchase{get;private set;} public ProductReviewStatus Status{get;private set;} public string? MerchantReply{get;private set;} public DateTimeOffset? PublishedAtUtc{get;private set;} public DateTimeOffset CreatedAtUtc{get;private set;} public Guid? CreatedByUserId{get;private set;} public DateTimeOffset? UpdatedAtUtc{get;private set;} public Guid? UpdatedByUserId{get;private set;}
 public static ProductReview Create(TenantId tenantId,ProductId productId,UserId customerUserId,OrderId orderId,int rating,string? body,bool verified,DateTimeOffset at,Guid? by=null)=>new(ProductReviewId.New(),tenantId,productId,customerUserId,orderId,rating,body,verified,at,by);
 public void Publish(DateTimeOffset at,Guid by){Status=ProductReviewStatus.Published;PublishedAtUtc=at;Touch(at,by);} public void Hide(DateTimeOffset at,Guid by){Status=ProductReviewStatus.Hidden;PublishedAtUtc=null;Touch(at,by);} public void Reply(string? reply,DateTimeOffset at,Guid by){MerchantReply=Norm(reply,MaxReplyLength);Touch(at,by);} 
 private void Touch(DateTimeOffset at,Guid by){UpdatedAtUtc=at;UpdatedByUserId=by;} private static int ValidateRating(int v){if(v<1||v>5)throw new ArgumentOutOfRangeException(nameof(v),"Rating must be between 1 and 5.");return v;} private static string? Norm(string? v,int max){if(string.IsNullOrWhiteSpace(v))return null;var n=v.Trim().Normalize(NormalizationForm.FormKC);if(n.Length>max)throw new ArgumentException($"Text cannot exceed {max} characters.");return n;}
}
