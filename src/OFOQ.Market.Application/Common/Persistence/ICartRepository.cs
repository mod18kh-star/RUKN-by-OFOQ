using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(
        CartId cartId,
        CancellationToken cancellationToken = default);

    Task<Cart?> GetActiveByCustomerUserIdAsync(
        UserId customerUserId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Cart cart,
        CancellationToken cancellationToken = default);
}