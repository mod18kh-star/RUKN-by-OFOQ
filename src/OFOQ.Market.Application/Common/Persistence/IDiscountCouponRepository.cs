using OFOQ.Market.Domain.Commerce.Discounts;
namespace OFOQ.Market.Application.Common.Persistence;
public interface IDiscountCouponRepository
{
    Task<IReadOnlyList<DiscountCoupon>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DiscountCoupon?> GetByIdAsync(DiscountCouponId id, CancellationToken cancellationToken = default);
    Task<DiscountCoupon?> GetByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string normalizedCode, DiscountCouponId? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(DiscountCoupon coupon, CancellationToken cancellationToken = default);
}
