using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Reviews;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Commerce.Reviews;

public sealed class ProductReviewTests
{
    [Fact]
    public void Published_review_can_receive_merchant_reply()
    {
        var now=DateTimeOffset.UtcNow;
        var review=ProductReview.Create(TenantId.From(Guid.NewGuid()),ProductId.From(Guid.NewGuid()),UserId.From(Guid.NewGuid()),OrderId.From(Guid.NewGuid()),5,"Great",true,now);
        review.Publish(now.AddMinutes(1),Guid.NewGuid());
        review.Reply("Thank you",now.AddMinutes(2),Guid.NewGuid());
        Assert.Equal(ProductReviewStatus.Published,review.Status);
        Assert.Equal("Thank you",review.MerchantReply);
        Assert.True(review.IsVerifiedPurchase);
    }

    [Fact]
    public void Rating_must_be_between_one_and_five()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>ProductReview.Create(TenantId.From(Guid.NewGuid()),ProductId.From(Guid.NewGuid()),UserId.From(Guid.NewGuid()),OrderId.From(Guid.NewGuid()),6,null,true,DateTimeOffset.UtcNow));
    }
}
