using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Carts.GetCart;

public sealed class GetCartHandler
{
    private readonly ICartRepository
        _cartRepository;

    private readonly ICurrentTenant
        _currentTenant;

    public GetCartHandler(
        ICartRepository cartRepository,
        ICurrentTenant currentTenant)
    {
        _cartRepository =
            cartRepository;

        _currentTenant =
            currentTenant;
    }

    public async Task<CartResult?> HandleAsync(
        UserId customerUserId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required.");
        }

        if (customerUserId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(customerUserId));
        }

        var cart =
            await _cartRepository
                .GetActiveByCustomerUserIdAsync(
                    customerUserId,
                    cancellationToken);

        if (cart is null)
        {
            return null;
        }

        return MapCart(
            cart);
    }

    private static CartResult MapCart(
        Cart cart)
    {
        var items =
            cart.Items
                .Select(
                    item =>
                        new CartItemResult(
                            item.Id,
                            item.ProductId,
                            item.ProductVariantId,
                            item.UnitPrice.Amount,
                            item.UnitPrice.Currency.Value,
                            item.Quantity,
                            item.LineTotal))
                .ToArray();

        return new CartResult(
            cart.Id,
            cart.Currency?.Value,
            cart.TotalQuantity,
            cart.TotalAmount,
            items);
    }
}