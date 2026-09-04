using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Carts;

namespace OFOQ.Market.Application.Commerce.Carts.ClearCart;

public sealed class ClearCartHandler
{
    private readonly ICartRepository
        _cartRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public ClearCartHandler(
        ICartRepository cartRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _cartRepository =
            cartRepository;

        _currentTenant =
            currentTenant;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<CartResult?> HandleAsync(
        ClearCartCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        EnsureTenantContext();

        if (command.CustomerUserId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(command));
        }

        var cart =
            await _cartRepository
                .GetActiveByCustomerUserIdAsync(
                    command.CustomerUserId,
                    cancellationToken);

        /*
         * DELETE /cart is intentionally idempotent.
         * No active cart means there is already
         * nothing to clear.
         */
        if (cart is null)
        {
            return null;
        }

        cart.Clear(
            _timeProvider.GetUtcNow(),
            command.CustomerUserId.Value);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return MapCart(
            cart);
    }

    private void EnsureTenantContext()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required.");
        }
    }

    private static CartResult MapCart(
        Cart cart)
    {
        return new CartResult(
            cart.Id,
            cart.Currency?.Value,
            cart.TotalQuantity,
            cart.TotalAmount,
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
                .ToArray());
    }
}