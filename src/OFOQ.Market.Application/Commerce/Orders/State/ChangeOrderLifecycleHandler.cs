using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Application.Commerce.Orders.State;

public sealed class ChangeOrderLifecycleHandler
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

    public ChangeOrderLifecycleHandler(
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

    public async Task<OrderLifecycleResult?> HandleAsync(
        ChangeOrderLifecycleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        EnsureTenant();

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

                if (IsIdempotentReplay(
                    order,
                    command))
                {
                    return Map(
                        order,
                        isIdempotentReplay: true);
                }

                var now =
                    _timeProvider.GetUtcNow();

                Apply(
                    order,
                    command,
                    now);

                await _unitOfWork
                    .SaveChangesAsync(
                        transactionCancellationToken);

                return Map(
                    order,
                    isIdempotentReplay: false);
            },
            cancellationToken);
    }

    private static void Apply(
        Order order,
        ChangeOrderLifecycleCommand command,
        DateTimeOffset now)
    {
        switch (command.Action)
        {
            case OrderLifecycleAction.Confirm:
                order.Confirm(
                    now,
                    command.ActorUserId.Value);
                break;

            case OrderLifecycleAction.StartProcessing:
                order.StartProcessing(
                    now,
                    command.ActorUserId.Value);
                break;

            case OrderLifecycleAction.ReadyToShip:
                order.MarkReadyToShip(
                    now,
                    command.ActorUserId.Value);
                break;

            case OrderLifecycleAction.Ship:
                order.MarkShipped(
                    command.ShippingCarrier
                    ?? throw new ArgumentException(
                        "Shipping carrier is required.",
                        nameof(command)),
                    command.TrackingNumber
                    ?? throw new ArgumentException(
                        "Tracking number is required.",
                        nameof(command)),
                    now,
                    command.ActorUserId.Value);
                break;

            case OrderLifecycleAction.MarkInTransit:
                order.MarkInTransit(
                    now,
                    command.ActorUserId.Value);
                break;

            case OrderLifecycleAction.Deliver:
                order.MarkDelivered(
                    now,
                    command.ActorUserId.Value);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(command.Action));
        }
    }

    private static bool IsIdempotentReplay(
        Order order,
        ChangeOrderLifecycleCommand command)
    {
        return command.Action switch
        {
            OrderLifecycleAction.Confirm =>
                order.Status ==
                OrderStatus.Confirmed,

            OrderLifecycleAction.StartProcessing =>
                order.Status ==
                    OrderStatus.Processing &&
                order.FulfillmentStatus ==
                    OrderFulfillmentStatus.Processing,

            OrderLifecycleAction.ReadyToShip =>
                order.Status ==
                    OrderStatus.Processing &&
                order.FulfillmentStatus ==
                    OrderFulfillmentStatus.ReadyToShip,

            OrderLifecycleAction.Ship =>
                order.FulfillmentStatus ==
                    OrderFulfillmentStatus.Shipped &&
                string.Equals(
                    order.ShippingCarrier,
                    command.ShippingCarrier?.Trim(),
                    StringComparison.Ordinal) &&
                string.Equals(
                    order.TrackingNumber,
                    command.TrackingNumber?.Trim(),
                    StringComparison.Ordinal),

            OrderLifecycleAction.MarkInTransit =>
                order.FulfillmentStatus ==
                OrderFulfillmentStatus.InTransit,

            OrderLifecycleAction.Deliver =>
                order.Status ==
                    OrderStatus.Fulfilled &&
                order.FulfillmentStatus ==
                    OrderFulfillmentStatus.Delivered,

            _ =>
                false
        };
    }

    private static OrderLifecycleResult Map(
        Order order,
        bool isIdempotentReplay)
    {
        return new OrderLifecycleResult(
            order.Id.Value,
            order.Status.ToString(),
            order.FulfillmentStatus.ToString(),
            order.ShippingCarrier,
            order.TrackingNumber,
            order.UpdatedAtUtc,
            isIdempotentReplay);
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to manage an order.");
        }
    }
}