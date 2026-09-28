using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Carts;

namespace OFOQ.Market.Application.Commerce.Carts.ClearCart;

public sealed class ClearCartHandler
{
    private readonly ICheckoutLockRepository
        _checkoutLockRepository;

    private readonly ITransactionExecutor
        _transactionExecutor;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    private readonly IStockHoldLedger? _holds;
    private readonly IOrderRepository? _orderRepository;

    public ClearCartHandler(
        ICheckoutLockRepository checkoutLockRepository,
        ITransactionExecutor transactionExecutor,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IStockHoldLedger? holds = null,
        IOrderRepository? orderRepository = null)
    {
        _checkoutLockRepository =
            checkoutLockRepository;

        _transactionExecutor =
            transactionExecutor;

        _currentTenant =
            currentTenant;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
        _holds = holds;
        _orderRepository = orderRepository;
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

        return await _transactionExecutor
            .ExecuteAsync(
                async transactionCancellationToken =>
                {
                    var cart =
                        await _checkoutLockRepository
                            .GetActiveCartForUpdateAsync(
                                command.CustomerUserId,
                                transactionCancellationToken);

                    /*
                     * DELETE /cart is intentionally idempotent.
                     * No active cart means there is already
                     * nothing to clear.
                     */
                    if (cart is null)
                    {
                        return null;
                    }

                    await PendingCheckoutCartGuard.EnsureEditableAsync(
                        cart, _orderRepository, transactionCancellationToken);

                    if (_holds?.Enabled == true)
                        await _holds.ReleaseCartAsync(cart.Id, transactionCancellationToken);

                    cart.Clear(
                        _timeProvider.GetUtcNow(),
                        command.CustomerUserId.Value);

                    await _unitOfWork.SaveChangesAsync(
                        transactionCancellationToken);

                    return MapCart(
                        cart);
                },
                cancellationToken);
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
