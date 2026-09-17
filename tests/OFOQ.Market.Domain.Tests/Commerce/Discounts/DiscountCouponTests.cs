using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Discounts;
using OFOQ.Market.Domain.Tenancy;
namespace OFOQ.Market.Domain.Tests.Commerce.Discounts;
public sealed class DiscountCouponTests
{
 [Fact] public void Percentage_CannotExceed100(){Assert.Throws<ArgumentOutOfRangeException>(()=>DiscountCoupon.Create(TenantId.New(),"SAVE","Save",DiscountCouponType.Percentage,101,DiscountCouponScope.EntireStore,CurrencyCode.Create("SAR"),false,null,null,null,null,null,DateTimeOffset.UtcNow));}
 [Fact] public void FixedDiscount_IsCappedAtEligibleAmount(){var x=DiscountCoupon.Create(TenantId.New(),"SAVE","Save",DiscountCouponType.FixedAmount,50,DiscountCouponScope.EntireStore,CurrencyCode.Create("SAR"),false,null,null,null,null,null,DateTimeOffset.UtcNow);Assert.Equal(20m,x.CalculateDiscount(20m));}
}
