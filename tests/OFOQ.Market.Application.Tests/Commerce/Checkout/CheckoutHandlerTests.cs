using OFOQ.Market.Application.Commerce.Checkout;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tests.Commerce.Checkout;

public sealed class CheckoutHandlerTests
{
    [Fact]
    public async Task HandleAsync_Success_RepricesCreatesPendingOrderDecreasesStockAndConvertsCart()
    {
        var setup =
            CreateSetup(
                productPrice: 25m,
                cartSnapshotPrice: 10m,
                stockQuantity: 8,
                requestedQuantity: 3);

        var result =
            await setup.Handler.HandleAsync(
                new CheckoutCommand(
                    setup.CustomerUserId,
                    "checkout-001"));

        Assert.False(
            result.IsIdempotentReplay);

        Assert.Equal(
            OrderStatus.Pending.ToString(),
            result.Status);

        Assert.Equal(
            75m,
            result.TotalAmount);

        var item =
            Assert.Single(
                result.Items);

        Assert.Equal(
            25m,
            item.UnitPrice);

        Assert.Equal(
            CartStatus.Converted,
            setup.Cart.Status);

        Assert.Equal(
            5,
            setup.Variant.Inventory.Quantity);

        var createdOrder =
            Assert.Single(
                setup.OrderRepository.Items)
                .Order;

        var movement =
            Assert.Single(
                createdOrder.InventoryMovements);

        Assert.Equal(
            InventoryMovementType.CheckoutDeduction,
            movement.Type);

        Assert.Equal(
            8,
            movement.QuantityBefore);

        Assert.Equal(
            5,
            movement.QuantityAfter);

        Assert.Equal(
            -3,
            movement.QuantityDelta);

        Assert.Single(
            setup.OrderRepository.Items);

        Assert.Equal(
            1,
            setup.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_SameIdempotencyKey_ReturnsSameOrderWithoutSecondStockDecrease()
    {
        var setup =
            CreateSetup(
                productPrice: 20m,
                cartSnapshotPrice: 20m,
                stockQuantity: 5,
                requestedQuantity: 2);

        var first =
            await setup.Handler.HandleAsync(
                new CheckoutCommand(
                    setup.CustomerUserId,
                    "retry-key"));

        var second =
            await setup.Handler.HandleAsync(
                new CheckoutCommand(
                    setup.CustomerUserId,
                    "retry-key"));

        Assert.Equal(
            first.OrderId,
            second.OrderId);

        Assert.True(
            second.IsIdempotentReplay);

        Assert.Equal(
            3,
            setup.Variant.Inventory.Quantity);

        Assert.Single(
            setup.OrderRepository.Items);

        Assert.Equal(
            1,
            setup.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_SameIdempotencyKeyWithNewActiveCart_ThrowsConflict()
    {
        var setup =
            CreateSetup(
                productPrice: 20m,
                cartSnapshotPrice: 20m,
                stockQuantity: 5,
                requestedQuantity: 1);

        await setup.Handler.HandleAsync(
            new CheckoutCommand(
                setup.CustomerUserId,
                "reused-key"));

        var newCart =
            Cart.Create(
                setup.TenantId,
                setup.CustomerUserId,
                setup.Now.AddMinutes(1),
                setup.CustomerUserId.Value);

        newCart.AddItem(
            setup.Product.Id,
            setup.Variant.Id,
            setup.Product.Price,
            1,
            setup.Now.AddMinutes(1),
            setup.CustomerUserId.Value);

        setup.CartRepository.Carts.Add(
            newCart);

        await Assert.ThrowsAsync<
            CheckoutIdempotencyConflictException>(
                () =>
                    setup.Handler.HandleAsync(
                        new CheckoutCommand(
                            setup.CustomerUserId,
                            "reused-key")));

        Assert.Single(
            setup.OrderRepository.Items);
    }

    [Fact]
    public async Task HandleAsync_InsufficientStock_DoesNotMutateAnything()
    {
        var setup =
            CreateSetup(
                productPrice: 20m,
                cartSnapshotPrice: 20m,
                stockQuantity: 1,
                requestedQuantity: 2);

        await Assert.ThrowsAsync<
            CheckoutInsufficientStockException>(
                () =>
                    setup.Handler.HandleAsync(
                        new CheckoutCommand(
                            setup.CustomerUserId,
                            "stock-fail")));

        Assert.Equal(
            CartStatus.Active,
            setup.Cart.Status);

        Assert.Equal(
            1,
            setup.Variant.Inventory.Quantity);

        Assert.Empty(
            setup.OrderRepository.Items);

        Assert.Equal(
            0,
            setup.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_UntrackedInventory_DoesNotDecreaseQuantity()
    {
        var setup =
            CreateSetup(
                productPrice: 12m,
                cartSnapshotPrice: 12m,
                stockQuantity: 0,
                requestedQuantity: 4,
                trackInventory: false);

        await setup.Handler.HandleAsync(
            new CheckoutCommand(
                setup.CustomerUserId,
                "untracked-stock"));

        Assert.Equal(
            0,
            setup.Variant.Inventory.Quantity);

        var untrackedOrder =
            Assert.Single(
                setup.OrderRepository.Items)
                .Order;

        Assert.Empty(
            untrackedOrder.InventoryMovements);

        Assert.Equal(
            CartStatus.Converted,
            setup.Cart.Status);
    }

    [Fact]
    public async Task HandleAsync_ContinueSellingWhenOutOfStock_ClampsQuantityAtZero()
    {
        var setup =
            CreateSetup(
                productPrice: 12m,
                cartSnapshotPrice: 12m,
                stockQuantity: 1,
                requestedQuantity: 4,
                continueSellingWhenOutOfStock: true);

        await setup.Handler.HandleAsync(
            new CheckoutCommand(
                setup.CustomerUserId,
                "oversell"));

        Assert.Equal(
            0,
            setup.Variant.Inventory.Quantity);

        var oversellOrder =
            Assert.Single(
                setup.OrderRepository.Items)
                .Order;

        var oversellMovement =
            Assert.Single(
                oversellOrder.InventoryMovements);

        Assert.Equal(
            InventoryMovementType.CheckoutDeduction,
            oversellMovement.Type);

        Assert.Equal(
            1,
            oversellMovement.QuantityBefore);

        Assert.Equal(
            0,
            oversellMovement.QuantityAfter);

        Assert.Equal(
            -1,
            oversellMovement.QuantityDelta);

        Assert.Equal(
            CartStatus.Converted,
            setup.Cart.Status);
    }

    private static CheckoutTestSetup CreateSetup(
        decimal productPrice,
        decimal cartSnapshotPrice,
        int stockQuantity,
        int requestedQuantity,
        bool trackInventory = true,
        bool continueSellingWhenOutOfStock = false)
    {
        var now =
            new DateTimeOffset(
                2026,
                9,
                6,
                10,
                0,
                0,
                TimeSpan.Zero);

        var tenantId =
            TenantId.New();

        var customerUserId =
            UserId.New();

        var product =
            Product.Create(
                tenantId,
                "Checkout Product",
                $"checkout-{Guid.NewGuid():N}",
                Money.Create(
                    productPrice,
                    "USD"),
                now,
                createdByUserId:
                    customerUserId.Value);

        product.Publish(
            now.AddSeconds(1),
            customerUserId.Value);

        var variant =
            ProductVariant.Create(
                tenantId,
                product.Id,
                "Default",
                ProductSku.Create(
                    $"SKU-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory,
                    stockQuantity,
                    continueSellingWhenOutOfStock:
                        continueSellingWhenOutOfStock),
                now,
                isDefault:
                    true,
                createdByUserId:
                    customerUserId.Value);

        var cart =
            Cart.Create(
                tenantId,
                customerUserId,
                now,
                customerUserId.Value);

        cart.AddItem(
            product.Id,
            variant.Id,
            Money.Create(
                cartSnapshotPrice,
                "USD"),
            requestedQuantity,
            now,
            customerUserId.Value);

        var cartRepository =
            new FakeCartRepository(
                cart);

        var checkoutLockRepository =
            new FakeCheckoutLockRepository(
                cartRepository,
                variant);

        var orderRepository =
            new FakeOrderRepository();

        var productRepository =
            new FakeProductRepository(
                product);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new CheckoutHandler(
                cartRepository,
                checkoutLockRepository,
                orderRepository,
                productRepository,
                new EmptyProductOptionRepository(),
                new EmptyVariantOptionValueRepository(),
                new FixedCurrentTenant(
                    tenantId),
                new InlineTransactionExecutor(),
                unitOfWork,
                new FixedTimeProvider(
                    now.AddMinutes(2)));

        return new CheckoutTestSetup(
            handler,
            tenantId,
            customerUserId,
            now,
            product,
            variant,
            cart,
            cartRepository,
            orderRepository,
            unitOfWork);
    }

    private sealed record CheckoutTestSetup(
        CheckoutHandler Handler,
        TenantId TenantId,
        UserId CustomerUserId,
        DateTimeOffset Now,
        Product Product,
        ProductVariant Variant,
        Cart Cart,
        FakeCartRepository CartRepository,
        FakeOrderRepository OrderRepository,
        FakeUnitOfWork UnitOfWork);

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

    private sealed class FakeCartRepository :
        ICartRepository
    {
        public FakeCartRepository(
            Cart cart)
        {
            Carts =
            [
                cart
            ];
        }

        public List<Cart> Carts { get; }

        public Task<Cart?> GetByIdAsync(
            CartId cartId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Carts.SingleOrDefault(
                    cart =>
                        cart.Id ==
                        cartId));
        }

        public Task<Cart?> GetActiveByCustomerUserIdAsync(
            UserId customerUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Carts.SingleOrDefault(
                    cart =>
                        cart.CustomerUserId ==
                        customerUserId &&
                        cart.Status ==
                        CartStatus.Active));
        }

        public Task AddAsync(
            Cart cart,
            CancellationToken cancellationToken = default)
        {
            Carts.Add(
                cart);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeCheckoutLockRepository :
        ICheckoutLockRepository
    {
        private readonly FakeCartRepository
            _cartRepository;

        private readonly ProductVariant
            _variant;

        public FakeCheckoutLockRepository(
            FakeCartRepository cartRepository,
            ProductVariant variant)
        {
            _cartRepository =
                cartRepository;

            _variant =
                variant;
        }

        public Task<Cart?> GetActiveCartForUpdateAsync(
            UserId customerUserId,
            CancellationToken cancellationToken = default)
        {
            return _cartRepository
                .GetActiveByCustomerUserIdAsync(
                    customerUserId,
                    cancellationToken);
        }

        public Task<IReadOnlyList<ProductVariant>>
            GetVariantsForUpdateAsync(
                IReadOnlyCollection<ProductVariantId> variantIds,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ProductVariant> variants =
                variantIds.Contains(
                    _variant.Id)
                    ? new[]
                    {
                        _variant
                    }
                    : Array.Empty<ProductVariant>();

            return Task.FromResult(
                variants);
        }
    }

    private sealed class FakeOrderRepository :
        IOrderRepository
    {
        public List<(Order Order, string? Key)> Items { get; } =
            [];

        public Task<Order?> GetByIdAsync(
            OrderId orderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items
                    .Select(
                        item =>
                            item.Order)
                    .SingleOrDefault(
                        order =>
                            order.Id ==
                            orderId));
        }

        public Task<Order?> GetBySourceCartIdAsync(
            CartId sourceCartId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items
                    .Select(
                        item =>
                            item.Order)
                    .SingleOrDefault(
                        order =>
                            order.SourceCartId ==
                            sourceCartId));
        }

        public Task<Order?> GetByCheckoutIdempotencyKeyAsync(
            UserId customerUserId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            var order =
                Items
                    .Where(
                        item =>
                            item.Order.CustomerUserId ==
                            customerUserId &&
                            string.Equals(
                                item.Key,
                                idempotencyKey,
                                StringComparison.Ordinal))
                    .Select(
                        item =>
                            item.Order)
                    .SingleOrDefault();

            return Task.FromResult(
                order);
        }

        public Task AddAsync(
            Order order,
            CancellationToken cancellationToken = default)
        {
            Items.Add(
                (order, null));

            return Task.CompletedTask;
        }

        public Task AddAsync(
            Order order,
            string checkoutIdempotencyKey,
            CancellationToken cancellationToken = default)
        {
            Items.Add(
                (order, checkoutIdempotencyKey));

            return Task.CompletedTask;
        }
    }

    private sealed class FakeProductRepository :
        IProductRepository
    {
        private readonly Product
            _product;

        public FakeProductRepository(
            Product product)
        {
            _product =
                product;
        }

        public Task<Product?> GetByIdAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Product?>(
                _product.Id ==
                productId
                    ? _product
                    : null);
        }

        public Task<Product?> GetBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Product?>(
                null);
        }

        public Task<IReadOnlyList<Product>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Product>>(
                new[]
                {
                    _product
                });
        }

        public Task<bool> SlugExistsAsync(
            string slug,
            ProductId? excludingProductId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                false);
        }

        public Task AddAsync(
            Product product,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class EmptyProductOptionRepository :
        IProductOptionRepository
    {
        public Task<ProductOption?> GetByIdAsync(
            ProductOptionId optionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ProductOption?>(
                null);
        }

        public Task<IReadOnlyList<ProductOption>> GetByProductIdAsync(
            ProductId productId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ProductOption>>(
                Array.Empty<ProductOption>());
        }

        public Task<bool> NameExistsAsync(
            ProductId productId,
            string normalizedName,
            ProductOptionId? excludingOptionId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                false);
        }

        public Task AddAsync(
            ProductOption option,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class EmptyVariantOptionValueRepository :
        IProductVariantOptionValueRepository
    {
        public Task<IReadOnlyList<ProductVariantOptionValue>>
            GetByVariantIdAsync(
                ProductVariantId variantId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ProductVariantOptionValue>>(
                Array.Empty<ProductVariantOptionValue>());
        }

        public Task<bool> ExistsForOptionAsync(
            ProductVariantId variantId,
            ProductOptionId optionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                false);
        }

        public Task AddAsync(
            ProductVariantOptionValue assignment,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
