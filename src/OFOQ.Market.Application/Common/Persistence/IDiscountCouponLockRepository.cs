using OFOQ.Market.Domain.Commerce.Discounts;
namespace OFOQ.Market.Application.Common.Persistence;
public interface IDiscountCouponLockRepository
{
    Task<DiscountCoupon?> GetByCodeForUpdateAsync(string normalizedCode, CancellationToken cancellationToken = default);
}
