using OFOQ.Market.Application.Commerce.Orders.Cancel;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tests.Commerce.Orders;

public sealed class CancelOrderHandlerTests
{
    [Fact]
    public async Task HandleAsync_CancelsOrderAndRestoresExactDeductedQuantity()
    {
        var setup =
            CreateSetup(
                stockQuantity: 8,
                requestedQuantity: 3);

        var result =
            await setup.Handler.HandleAsync(
                new CancelOrderCommand(
                    setup.Order.Id,
                    "Customer requested cancellation",
                    setup.ActorUserId));

        Assert.NotNull(
            result);

        Assert.False(
            result.IsIdempotentReplay);

        Assert.Equal(
            OrderStatus.Cancelled,
            setup.Order.Status);

        Assert.Equal(
            OrderFulfillmentStatus.Cancelled,
            setup.Order.FulfillmentStatus);

        Assert.Equal(
            8,
            setup.Variant.Inventory.Quantity);

        var restock =
            Assert.Single(
                result.Inventory);

        Assert.Equal(
            setup.Variant.Id,
            restock.ProductVariantId);

        Assert.Equal(
            3,
            restock.RestoredQuantity);

        Assert.Equal(
            8,
            restock.QuantityAfter);

        Assert.Equal(
            2,
            setup.Order.InventoryMovements.Count);

        Assert.Single(
            setup.Order.InventoryMovements.Where(
                movement =>
                    movement.Type ==
                    InventoryMovementType.CheckoutDeduction));

        Assert.Single(
            setup.Order.InventoryMovements.Where(
                movement =>
                    movement.Type ==
                    InventoryMovementType.OrderCancellationRestock));

        Assert.Equal(
            1,
            setup.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_RepeatedCancellation_DoesNotRestockTwice()
    {
        var setup =
            CreateSetup(
                stockQuantity: 8,
                requestedQuantity: 3);

        var first =
            await setup.Handler.HandleAsync(
                new CancelOrderCommand(
                    setup.Order.Id,
                    "Cancelled",
                    setup.ActorUserId));

        var quantityAfterFirstCancellation =
            setup.Variant.Inventory.Quantity;

        var movementCountAfterFirstCancellation =
            setup.Order.InventoryMovements.Count;

        var second =
            await setup.Handler.HandleAsync(
                new CancelOrderCommand(
                    setup.Order.Id,
                    "Cancelled again",
                    setup.ActorUserId));

        Assert.NotNull(
            first);

        Assert.NotNull(
            second);

        Assert.False(
            first.IsIdempotentReplay);

        Assert.True(
            second.IsIdempotentReplay);

        Assert.Equal(
            8,
            quantityAfterFirstCancellation);

        Assert.Equal(
            quantityAfterFirstCancellation,
            setup.Variant.Inventory.Quantity);

        Assert.Equal(
            movementCountAfterFirstCancellation,
            setup.Order.InventoryMovements.Count);

        Assert.Equal(
            2,
            setup.Order.InventoryMovements.Count);

        Assert.Equal(
            1,
            setup.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_OversoldOrder_RestoresOnlyActuallyDeductedQuantity()
    {
        var setup =
            CreateSetup(
                stockQuantity: 1,
                requestedQuantity: 4,
                continueSellingWhenOutOfStock: true);

        Assert.Equal(
            0,
            setup.Variant.Inventory.Quantity);

        var deduction =
            Assert.Single(
                setup.Order.InventoryMovements);

        Assert.Equal(
            -1,
            deduction.QuantityDelta);

        var result =
            await setup.Handler.HandleAsync(
                new CancelOrderCommand(
                    setup.Order.Id,
                    "Oversold order cancelled",
                    setup.ActorUserId));

        Assert.NotNull(
            result);

        Assert.Equal(
            1,
            setup.Variant.Inventory.Quantity);

        var restock =
            Assert.Single(
                result.Inventory);

        Assert.Equal(
            1,
            restock.RestoredQuantity);

        Assert.Equal(
            1,
            restock.QuantityAfter);

        var restockMovement =
            setup.Order.InventoryMovements.Single(
                movement =>
                    movement.Type ==
                    InventoryMovementType.OrderCancellationRestock);

        Assert.Equal(
            1,
            restockMovement.QuantityDelta);
    }

    [Fact]
    public async Task HandleAsync_ShippedOrder_RejectsCancellationWithoutRestocking()
    {
        var setup =
            CreateSetup(
                stockQuantity: 8,
                requestedQuantity: 3);

        setup.Order.MarkPaid(
            setup.Now.AddMinutes(2),
            setup.ActorUserId.Value);

        setup.Order.Confirm(
            setup.Now.AddMinutes(3),
            setup.ActorUserId.Value);

        setup.Order.StartProcessing(
            setup.Now.AddMinutes(4),
            setup.ActorUserId.Value);

        setup.Order.MarkReadyToShip(
            setup.Now.AddMinutes(5),
            setup.ActorUserId.Value);

        setup.Order.MarkShipped(
            "Aramex",
            "TRACK-001",
            setup.Now.AddMinutes(6),
            setup.ActorUserId.Value);

        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () =>
                    setup.Handler.HandleAsync(
                        new CancelOrderCommand(
                            setup.Order.Id,
                            "Too late",
                            setup.ActorUserId)));

        Assert.Equal(
            OrderStatus.Processing,
            setup.Order.Status);

        Assert.Equal(
            OrderFulfillmentStatus.Shipped,
            setup.Order.FulfillmentStatus);

        Assert.Equal(
            5,
            setup.Variant.Inventory.Quantity);

        Assert.Single(
            setup.Order.InventoryMovements);

        Assert.Equal(
            0,
            setup.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_OrderNotFound_ReturnsNull()
    {
        var setup =
            CreateSetup(
                stockQuantity: 5,
                requestedQuantity: 1);

        setup.StateLockRepository.Order =
            null;

        var result =
            await setup.Handler.HandleAsync(
                new CancelOrderCommand(
                    OrderId.New(),
                    null,
                    setup.ActorUserId));

        Assert.Null(
            result);

        Assert.Equal(
            0,
            setup.UnitOfWork.SaveCount);
    }

    private static CancelOrderTestSetup CreateSetup(
        int stockQuantity,
        int requestedQuantity,
        bool continueSellingWhenOutOfStock = false)
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                14,
                12,
                0,
                0,
                TimeSpan.Zero);

        var tenantId =
            TenantId.New();

        var customerUserId =
            UserId.New();

        var actorUserId =
            UserId.New();

        var product =
            Product.Create(
                tenantId,
                "Cancellation Product",
                $"cancel-{Guid.NewGuid():N}",
                Money.Create(
                    100m,
                    "SAR"),
                now,
                createdByUserId:
                    actorUserId.Value);

        var variant =
            ProductVariant.Create(
                tenantId,
                product.Id,
                "Default",
                ProductSku.Create(
                    $"SKU-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "SAR"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: stockQuantity,
                    lowStockThreshold: 2,
                    continueSellingWhenOutOfStock:
                        continueSellingWhenOutOfStock),
                now,
                isDefault: true,
                createdByUserId:
                    actorUserId.Value);

        var order =
            Order.Create(
                tenantId,
                customerUserId,
                CartId.New(),
                CurrencyCode.Create(
                    "SAR"),
                new[]
                {
                    new OrderItemSnapshot(
                        product.Id,
                        variant.Id,
                        product.Name,
                        variant.Name,
                        variant.Sku.Value,
                        Money.Create(
                            100m,
                            "SAR"),
                        requestedQuantity)
                },
                now,
                customerUserId.Value);

        var quantityBefore =
            variant.Inventory.Quantity;

        variant.DecreaseStock(
            requestedQuantity,
            now.AddMinutes(1),
            customerUserId.Value);

        var quantityAfter =
            variant.Inventory.Quantity;

        order.RecordCheckoutInventoryDeduction(
            product.Id,
            variant.Id,
            quantityBefore,
            quantityAfter,
            now.AddMinutes(1),
            customerUserId.Value);

        var stateLockRepository =
            new FakeOrderStateLockRepository(
                order,
                variant);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CancelOrderHandler(
                stateLockRepository,
                new FixedCurrentTenant(
                    tenantId),
                new InlineTransactionExecutor(),
                unitOfWork,
                new FixedTimeProvider(
                    now.AddMinutes(10)));

        return new CancelOrderTestSetup(
            handler,
            stateLockRepository,
            unitOfWork,
            order,
            variant,
            actorUserId,
            now);
    }

    private sealed record CancelOrderTestSetup(
        CancelOrderHandler Handler,
        FakeOrderStateLockRepository StateLockRepository,
        FakeUnitOfWork UnitOfWork,
        Order Order,
        ProductVariant Variant,
        UserId ActorUserId,
        DateTimeOffset Now);

    private sealed class FakeOrderStateLockRepository :
        IOrderStateLockRepository
    {
        private readonly ProductVariant
            _variant;

        public FakeOrderStateLockRepository(
            Order order,
            ProductVariant variant)
        {
            Order =
                order;

            _variant =
                variant;
        }

        public Order? Order { get; set; }

        public Task<Order?> GetOrderForUpdateAsync(
            OrderId orderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Order is not null &&
                Order.Id ==
                orderId
                    ? Order
                    : null);
        }

        public Task<IReadOnlyList<ProductVariant>>
            GetVariantsForUpdateAsync(
                IReadOnlyCollection<ProductVariantId> variantIds,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ProductVariant> result =
                variantIds.Contains(
                    _variant.Id)
                    ? new[]
                    {
                        _variant
                    }
                    : Array.Empty<ProductVariant>();

            return Task.FromResult(
                result);
        }
    }

    private sealed class FixedCurrentTenant :
        ICurrentTenant
    {
        public FixedCurrentTenant(
            TenantId tenantId)
        {
            TenantId =
                tenantId;
        }

        public TenantId? TenantId { get; }
    }

    private sealed class InlineTransactionExecutor :
        ITransactionExecutor
    {
        public Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            return operation(
                cancellationToken);
        }
    }

    private sealed class FakeUnitOfWork :
        IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;

            return Task.FromResult(
                1);
        }
    }

    private sealed class FixedTimeProvider :
        TimeProvider
    {
        private readonly DateTimeOffset
            _utcNow;

        public FixedTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow =
                utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}