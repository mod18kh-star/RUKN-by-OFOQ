using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Application.Commerce.Orders.Cancel;

public sealed class CancelOrderHandler
{
    private readonly IOrderStateLockRepository
        _stateLockRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly ITransactionExecutor
        _transactionExecutor;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public CancelOrderHandler(
        IOrderStateLockRepository stateLockRepository,
        ICurrentTenant currentTenant,
        ITransactionExecutor transactionExecutor,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _stateLockRepository =
            stateLockRepository;

        _currentTenant =
            currentTenant;

        _transactionExecutor =
            transactionExecutor;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<CancelOrderResult?> HandleAsync(
        CancelOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        EnsureTenantContext();

        if (command.OrderId.IsEmpty)
        {
            throw new ArgumentException(
                "Order ID cannot be empty.",
                nameof(command));
        }

        if (command.ActorUserId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Actor user ID cannot be empty.",
                nameof(command));
        }

        return await _transactionExecutor.ExecuteAsync(
            async transactionCancellationToken =>
            {
                var order =
                    await _stateLockRepository
                        .GetOrderForUpdateAsync(
                            command.OrderId,
                            transactionCancellationToken);

                if (order is null)
                {
                    return null;
                }

                if (order.Status ==
                    OrderStatus.Cancelled)
                {
                    return Map(
                        order,
                        isIdempotentReplay: true);
                }

                var deductions =
                    order.InventoryMovements
                        .Where(
                            movement =>
                                movement.Type ==
                                InventoryMovementType.CheckoutDeduction &&
                                movement.QuantityDelta <
                                0)
                        .ToArray();

                var variantIds =
                    deductions
                        .Select(
                            movement =>
                                movement.ProductVariantId)
                        .Distinct()
                        .ToArray();

                var lockedVariants =
                    await _stateLockRepository
                        .GetVariantsForUpdateAsync(
                            variantIds,
                            transactionCancellationToken);

                if (lockedVariants.Count !=
                    variantIds.Length)
                {
                    throw new OrderCancellationInventoryStateException();
                }

                var variantsById =
                    lockedVariants.ToDictionary(
                        variant =>
                            variant.Id);

                var now =
                    _timeProvider.GetUtcNow();

                order.Cancel(
                    command.Reason,
                    now,
                    command.ActorUserId.Value);

                foreach (var deduction in
                         deductions)
                {
                    var quantityToRestore =
                        order.GetCancellationRestockQuantity(
                            deduction.ProductVariantId);

                    if (quantityToRestore <= 0)
                    {
                        continue;
                    }

                    if (!variantsById.TryGetValue(
                            deduction.ProductVariantId,
                            out var variant))
                    {
                        throw new OrderCancellationInventoryStateException();
                    }

                    var quantityBefore =
                        variant.Inventory.Quantity;

                    variant.RestoreStockFromOrderCancellation(
                        quantityToRestore,
                        now,
                        command.ActorUserId.Value);

                    var quantityAfter =
                        variant.Inventory.Quantity;

                    order.RecordCancellationInventoryRestock(
                        deduction.ProductId,
                        deduction.ProductVariantId,
                        quantityBefore,
                        quantityAfter,
                        now,
                        command.ActorUserId.Value);
                }

                await _unitOfWork
                    .SaveChangesAsync(
                        transactionCancellationToken);

                return Map(
                    order,
                    isIdempotentReplay: false);
            },
            cancellationToken);
    }

    private static CancelOrderResult Map(
        Order order,
        bool isIdempotentReplay)
    {
        var restocks =
            order.InventoryMovements
                .Where(
                    movement =>
                        movement.Type ==
                        InventoryMovementType.OrderCancellationRestock)
                .Select(
                    movement =>
                        new CancelOrderInventoryResult(
                            movement.ProductVariantId,
                            movement.QuantityDelta,
                            movement.QuantityAfter))
                .ToArray();

        return new CancelOrderResult(
            order.Id,
            order.Status,
            order.FulfillmentStatus,
            order.CancellationReason,
            order.CancelledAtUtc,
            isIdempotentReplay,
            restocks);
    }

    private void EnsureTenantContext()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to cancel an order.");
        }
    }
}