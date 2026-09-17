using OFOQ.Market.Domain.Commerce.Discounts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
namespace OFOQ.Market.Domain.Tests.Commerce.Discounts;
public sealed class CouponRedemptionTests
{
 [Fact] public void Create_RejectsZeroDiscount(){Assert.Throws<ArgumentOutOfRangeException>(()=>CouponRedemption.Create(TenantId.New(),DiscountCouponId.New(),OrderId.New(),UserId.New(),0m,DateTimeOffset.UtcNow));}
}
