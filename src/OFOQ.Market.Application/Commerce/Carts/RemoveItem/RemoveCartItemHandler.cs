using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Carts;

namespace OFOQ.Market.Application.Commerce.Carts.RemoveItem;

public sealed class RemoveCartItemHandler
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

    public RemoveCartItemHandler(
        ICheckoutLockRepository checkoutLockRepository,
        ITransactionExecutor transactionExecutor,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
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
    }

    public async Task<CartResult> HandleAsync(
        RemoveCartItemCommand command,
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

        if (command.CartItemId.IsEmpty)
        {
            throw new ArgumentException(
                "Cart item ID cannot be empty.",
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
                                transactionCancellationToken)
                        ?? throw new CartNotFoundException();

                    if (!cart.Items.Any(
                            item =>
                                item.Id ==
                                command.CartItemId))
                    {
                        throw new CartItemNotFoundException();
                    }

                    var now =
                        _timeProvider.GetUtcNow();

                    cart.RemoveItem(
                        command.CartItemId,
                        now,
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
