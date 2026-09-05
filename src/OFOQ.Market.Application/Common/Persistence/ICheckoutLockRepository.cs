using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ICheckoutLockRepository
{
    Task<Cart?> GetActiveCartForUpdateAsync(
        UserId customerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductVariant>>
        GetVariantsForUpdateAsync(
            IReadOnlyCollection<ProductVariantId> variantIds,
            CancellationToken cancellationToken = default);
}