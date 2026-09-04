using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class CartRepository :
    ICartRepository
{
    private readonly MarketDbContext _dbContext;

    public CartRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<Cart?> GetByIdAsync(
        CartId cartId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Carts
            .Include("_items")
            .SingleOrDefaultAsync(
                cart =>
                    cart.Id == cartId,
                cancellationToken);
    }

    public Task<Cart?> GetActiveByCustomerUserIdAsync(
        UserId customerUserId,
        CancellationToken cancellationToken = default)
    {
        if (customerUserId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(customerUserId));
        }

        var customerId =
            customerUserId.Value;

        return _dbContext.Carts
            .Include("_items")
            .Where(
                cart =>
                    cart.Status ==
                    CartStatus.Active)
            .SingleOrDefaultAsync(
                cart =>
                    EF.Property<Guid?>(
                        cart,
                        "_customerUserId") ==
                    customerId,
                cancellationToken);
    }

    public Task AddAsync(
        Cart cart,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            cart);

        return _dbContext.Carts
            .AddAsync(
                cart,
                cancellationToken)
            .AsTask();
    }
}