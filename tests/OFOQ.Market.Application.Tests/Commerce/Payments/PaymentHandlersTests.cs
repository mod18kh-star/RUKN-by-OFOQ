using OFOQ.Market.Application.Commerce.Payments.Common;
using OFOQ.Market.Application.Commerce.Payments.CreateIntent;
using OFOQ.Market.Application.Commerce.Payments.GetAvailableMethods;
using OFOQ.Market.Application.Commerce.Payments.RetryIntent;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tests.Commerce.Payments;

public sealed class PaymentHandlersTests
{
    [Fact]
    public async Task AvailableMethods_FiltersCodAndSuspendedElectronicMethods()
    {
        var setup = CreateSetup();
        var electronic = CreateMethod(setup, PaymentMethodType.Electronic, "electronic");
        var manual = CreateMethod(setup, PaymentMethodType.ManualTransfer, "manual");
        var cod = CreateMethod(setup, PaymentMethodType.CashOnDelivery, "cod");
        setup.Methods.Items.AddRange(new[] { electronic, manual, cod });

        var capability = TenantPaymentCapability.Create(
            setup.TenantId,
            setup.Now,
            setup.CustomerUserId.Value);

        capability.SuspendElectronicPayments(
            "test",
            setup.Now,
            setup.CustomerUserId.Value);

        setup.Capabilities.Item = capability;

        var handler = new GetAvailablePaymentMethodsHandler(
            setup.Orders,
            setup.Payments,
            setup.Methods,
            setup.Capabilities,
            setup.CurrentTenant);

        var result = await handler.HandleAsync(
            new GetAvailablePaymentMethodsQuery(
                setup.Order.Id,
                setup.CustomerUserId));

        var method = Assert.Single(result);

        Assert.Equal("manual", method.ProviderCode);
    }

    [Fact]
    public async Task CreateIntent_SameIdempotencyKey_ReplaysSameIntent()
    {
        var setup = CreateSetup();

        var method = CreateMethod(
            setup,
            PaymentMethodType.Electronic,
            "provider-a");

        setup.Methods.Items.Add(method);

        var handler = CreateHandler(setup);

        var command = new CreatePaymentIntentCommand(
            setup.Order.Id,
            setup.CustomerUserId,
            method.Id,
            "same-key");

        var first = await handler.HandleAsync(command);
        var second = await handler.HandleAsync(command);

        Assert.Equal(first.PaymentIntentId, second.PaymentIntentId);
        Assert.False(first.IsIdempotentReplay);
        Assert.True(second.IsIdempotentReplay);
        Assert.Single(setup.Payments.Items);
        Assert.Single(setup.Intents.Items);
    }

    [Fact]
    public async Task CreateIntent_SameKeyForDifferentMethod_ThrowsConflict()
    {
        var setup = CreateSetup();

        var firstMethod = CreateMethod(
            setup,
            PaymentMethodType.Electronic,
            "first");

        var secondMethod = CreateMethod(
            setup,
            PaymentMethodType.Electronic,
            "second");

        setup.Methods.Items.AddRange(
            new[]
            {
                firstMethod,
                secondMethod
            });

        var handler = CreateHandler(setup);

        await handler.HandleAsync(
            new CreatePaymentIntentCommand(
                setup.Order.Id,
                setup.CustomerUserId,
                firstMethod.Id,
                "reused-key"));

        await Assert.ThrowsAsync<PaymentIdempotencyConflictException>(
            () => handler.HandleAsync(
                new CreatePaymentIntentCommand(
                    setup.Order.Id,
                    setup.CustomerUserId,
                    secondMethod.Id,
                    "reused-key")));
    }

    [Fact]
    public async Task Retry_PendingIntent_IsRejected()
    {
        var setup = CreateSetup();

        var method = CreateMethod(
            setup,
            PaymentMethodType.Electronic,
            "provider-a");

        setup.Methods.Items.Add(method);

        var create = CreateHandler(setup);

        var created = await create.HandleAsync(
            new CreatePaymentIntentCommand(
                setup.Order.Id,
                setup.CustomerUserId,
                method.Id,
                "create-key"));

        var retry = CreateRetryHandler(setup);

        await Assert.ThrowsAsync<PaymentIntentNotRetryableException>(
            () => retry.HandleAsync(
                new RetryPaymentIntentCommand(
                    created.PaymentIntentId,
                    setup.CustomerUserId,
                    "retry-key")));
    }

    [Fact]
    public async Task Retry_FailedIntent_CreatesNewIntentOnSamePayment()
    {
        var setup = CreateSetup();

        var method = CreateMethod(
            setup,
            PaymentMethodType.Electronic,
            "provider-a");

        setup.Methods.Items.Add(method);

        var create = CreateHandler(setup);

        var created = await create.HandleAsync(
            new CreatePaymentIntentCommand(
                setup.Order.Id,
                setup.CustomerUserId,
                method.Id,
                "create-key"));

        var source = Assert.Single(setup.Intents.Items).Intent;

        source.MarkFailed(
            null,
            setup.Now.AddSeconds(1),
            setup.CustomerUserId.Value);

        var retry = CreateRetryHandler(setup);

        var result = await retry.HandleAsync(
            new RetryPaymentIntentCommand(
                created.PaymentIntentId,
                setup.CustomerUserId,
                "retry-key"));

        Assert.NotEqual(
            created.PaymentIntentId,
            result.PaymentIntentId);

        Assert.Equal(
            created.PaymentId,
            result.PaymentId);

        Assert.Equal(
            "Pending",
            result.Status);

        Assert.Equal(
            PaymentIntentStatus.Failed,
            source.Status);

        Assert.Equal(
            2,
            setup.Intents.Items.Count);
    }

    private static CreatePaymentIntentHandler CreateHandler(
        TestSetup setup)
    {
        return new CreatePaymentIntentHandler(
            setup.Payments,
            setup.Intents,
            setup.Methods,
            setup.Capabilities,
            new FakePaymentCreationLockRepository(setup.Orders),
            setup.CurrentTenant,
            new FakeTransactionExecutor(),
            new FakeUnitOfWork(),
            new FixedTimeProvider(setup.Now));
    }

    private static RetryPaymentIntentHandler CreateRetryHandler(
        TestSetup setup)
    {
        return new RetryPaymentIntentHandler(
            setup.Intents,
            setup.Payments,
            setup.Methods,
            setup.Capabilities,
            new FakePaymentCreationLockRepository(setup.Orders),
            setup.CurrentTenant,
            new FakeTransactionExecutor(),
            new FakeUnitOfWork(),
            new FixedTimeProvider(setup.Now.AddMinutes(1)));
    }

    private static TestSetup CreateSetup()
    {
        var now = new DateTimeOffset(
            2026,
            9,
            6,
            12,
            0,
            0,
            TimeSpan.Zero);

        var tenantId = TenantId.New();
        var customerUserId = UserId.New();

        var order = Order.Create(
            tenantId,
            customerUserId,
            CartId.New(),
            CurrencyCode.Create("USD"),
            new[]
            {
                new OrderItemSnapshot(
                    ProductId.New(),
                    ProductVariantId.New(),
                    "Product",
                    "Default",
                    "SKU-1",
                    Money.Create(50m, "USD"),
                    1)
            },
            now,
            customerUserId.Value);

        var orders = new FakeOrderRepository();

        orders.Items.Add(order);

        return new TestSetup(
            now,
            tenantId,
            customerUserId,
            order,
            new FakeCurrentTenant(tenantId),
            orders,
            new FakePaymentRepository(),
            new FakePaymentIntentRepository(),
            new FakePaymentMethodRepository(),
            new FakeCapabilityRepository());
    }

    private static TenantPaymentMethod CreateMethod(
        TestSetup setup,
        PaymentMethodType type,
        string providerCode)
    {
        return TenantPaymentMethod.Create(
            setup.TenantId,
            type,
            providerCode,
            providerCode,
            "SY",
            "USD",
            null,
            null,
            setup.Now,
            setup.CustomerUserId.Value);
    }

    private sealed record TestSetup(
        DateTimeOffset Now,
        TenantId TenantId,
        UserId CustomerUserId,
        Order Order,
        FakeCurrentTenant CurrentTenant,
        FakeOrderRepository Orders,
        FakePaymentRepository Payments,
        FakePaymentIntentRepository Intents,
        FakePaymentMethodRepository Methods,
        FakeCapabilityRepository Capabilities);

    private sealed class FakeCurrentTenant : ICurrentTenant
    {
        public FakeCurrentTenant(
            TenantId tenantId)
        {
            TenantId = tenantId;
        }

        public TenantId? TenantId { get; }
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        public List<Order> Items { get; } = [];

        public Task<Order?> GetByIdAsync(
            OrderId orderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items.SingleOrDefault(
                    item => item.Id == orderId));
        }

        public Task<Order?> GetBySourceCartIdAsync(
            CartId sourceCartId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items.SingleOrDefault(
                    item => item.SourceCartId == sourceCartId));
        }

        public Task<Order?> GetByCheckoutIdempotencyKeyAsync(
            UserId customerUserId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Order?>(null);
        }

        public Task AddAsync(
            Order order,
            CancellationToken cancellationToken = default)
        {
            Items.Add(order);

            return Task.CompletedTask;
        }

        public Task AddAsync(
            Order order,
            string checkoutIdempotencyKey,
            CancellationToken cancellationToken = default)
        {
            return AddAsync(
                order,
                cancellationToken);
        }
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        public List<Payment> Items { get; } = [];

        public Task<Payment?> GetByIdAsync(
            PaymentId paymentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items.SingleOrDefault(
                    item => item.Id == paymentId));
        }

        public Task<Payment?> GetByOrderIdAsync(
            OrderId orderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items.SingleOrDefault(
                    item => item.OrderId == orderId));
        }

        public Task AddAsync(
            Payment payment,
            CancellationToken cancellationToken = default)
        {
            Items.Add(payment);

            return Task.CompletedTask;
        }
    }

    private sealed record IntentEntry(
        PaymentIntent Intent,
        string Key);

    private sealed class FakePaymentIntentRepository
        : IPaymentIntentRepository
    {
        public List<IntentEntry> Items { get; } = [];

        public Task<PaymentIntent?> GetByIdAsync(
            PaymentIntentId paymentIntentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items
                    .Select(item => item.Intent)
                    .SingleOrDefault(
                        item => item.Id == paymentIntentId));
        }

        public Task<PaymentIntent?> GetByCreateIdempotencyKeyAsync(
            UserId customerUserId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items
                    .SingleOrDefault(
                        item =>
                            item.Intent.CustomerUserId == customerUserId &&
                            item.Key == idempotencyKey)
                    ?.Intent);
        }

        public Task<PaymentIntent?> GetByProviderReferenceAsync(
            TenantPaymentMethodId tenantPaymentMethodId,
            string providerReference,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items
                    .Select(item => item.Intent)
                    .SingleOrDefault(
                        item =>
                            item.TenantPaymentMethodId == tenantPaymentMethodId &&
                            item.ProviderReference == providerReference));
        }

        public Task<IReadOnlyList<PaymentIntent>> GetByPaymentIdAsync(
            PaymentId paymentId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PaymentIntent> intents =
                Items
                    .Select(item => item.Intent)
                    .Where(
                        item => item.PaymentId == paymentId)
                    .OrderBy(
                        item => item.CreatedAtUtc)
                    .ToArray();

            return Task.FromResult(intents);
        }

        public Task AddAsync(
            PaymentIntent paymentIntent,
            string createIdempotencyKey,
            CancellationToken cancellationToken = default)
        {
            Items.Add(
                new IntentEntry(
                    paymentIntent,
                    createIdempotencyKey));

            return Task.CompletedTask;
        }
    }

    private sealed class FakePaymentMethodRepository
        : ITenantPaymentMethodRepository
    {
        public List<TenantPaymentMethod> Items { get; } = [];

        public Task<TenantPaymentMethod?> GetByIdAsync(
            TenantPaymentMethodId paymentMethodId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items.SingleOrDefault(
                    item => item.Id == paymentMethodId));
        }

        public Task<IReadOnlyList<TenantPaymentMethod>> GetEnabledAsync(
            CurrencyCode currency,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TenantPaymentMethod> result = Items
                .Where(
                    item =>
                        item.IsEnabled &&
                        item.Currency == currency)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task AddAsync(
            TenantPaymentMethod paymentMethod,
            CancellationToken cancellationToken = default)
        {
            Items.Add(paymentMethod);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeCapabilityRepository
        : ITenantPaymentCapabilityRepository
    {
        public TenantPaymentCapability? Item { get; set; }

        public Task<TenantPaymentCapability?> GetAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Item);
        }

        public Task AddAsync(
            TenantPaymentCapability capability,
            CancellationToken cancellationToken = default)
        {
            Item = capability;

            return Task.CompletedTask;
        }
    }

    private sealed class FakePaymentCreationLockRepository
        : IPaymentCreationLockRepository
    {
        private readonly FakeOrderRepository _orders;

        public FakePaymentCreationLockRepository(
            FakeOrderRepository orders)
        {
            _orders = orders;
        }

        public Task<Order?> GetOrderForUpdateAsync(
            OrderId orderId,
            UserId customerUserId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            return _orders.GetByIdAsync(
                orderId,
                cancellationToken);
        }
    }

    private sealed class FakeTransactionExecutor
        : ITransactionExecutor
    {
        public Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            return operation(cancellationToken);
        }
    }

    private sealed class FakeUnitOfWork
        : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FixedTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(
            DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}