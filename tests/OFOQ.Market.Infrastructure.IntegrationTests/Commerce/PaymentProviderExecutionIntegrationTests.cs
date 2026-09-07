using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Application.Commerce.Payments.ExecuteIntent;
using OFOQ.Market.Application.Commerce.Payments.ProcessWebhook;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;
using OFOQ.Market.Infrastructure.Persistence.Repositories;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class PaymentProviderExecutionIntegrationTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task ExecuteIntent_ConcurrentCalls_InvokeProviderOnce()
    {
        await _database.ResetAsync();
        var seed = await SeedPaymentIntentAsync(markProcessing: false);
        var provider = new TestProvider(seed.Intent.Id);

        await using var contextA = CreateConcurrentContext(seed.TenantId);
        await using var contextB = CreateConcurrentContext(seed.TenantId);

        var handlerA = CreateExecuteHandler(contextA, seed.TenantId, provider);
        var handlerB = CreateExecuteHandler(contextB, seed.TenantId, provider);

        var command = new ExecutePaymentIntentCommand(
            seed.Intent.Id,
            seed.CustomerUserId);

        var results = await Task.WhenAll(
            handlerA.HandleAsync(command),
            handlerB.HandleAsync(command));

        Assert.Equal(1, provider.CreatePaymentCallCount);
        Assert.All(results, result =>
            Assert.Contains(
                result.Status,
                new[] { "Processing", "RequiresAction" }));

        await using var verificationContext = _database.CreateContext(
            new TestCurrentTenant(seed.TenantId));

        var intent = await verificationContext.PaymentIntents
            .SingleAsync(item => item.Id == seed.Intent.Id);

        Assert.Equal(PaymentIntentStatus.RequiresAction, intent.Status);
    }

    [Fact]
    public async Task Webhook_ConcurrentDuplicateEvent_IsAppliedOnce()
    {
        await _database.ResetAsync();
        var seed = await SeedPaymentIntentAsync(markProcessing: true);
        var provider = new TestProvider(seed.Intent.Id)
        {
            WebhookResult = new PaymentWebhookResult(
                true,
                "integration-event-1",
                PaymentIntentStatus.Succeeded,
                "integration-provider-reference",
                seed.Intent.Id,
                seed.Payment.Amount,
                seed.Payment.Currency.Value)
        };

        await using var contextA = CreateConcurrentContext(seed.TenantId);
        await using var contextB = CreateConcurrentContext(seed.TenantId);

        var handlerA = CreateWebhookHandler(contextA, seed.TenantId, provider);
        var handlerB = CreateWebhookHandler(contextB, seed.TenantId, provider);

        var command = new ProcessPaymentWebhookCommand(
            seed.Method.Id,
            "{}"u8.ToArray(),
            new Dictionary<string, string>());

        var results = await Task.WhenAll(
            handlerA.HandleAsync(command),
            handlerB.HandleAsync(command));

        Assert.Single(results, item => item.Applied);
        Assert.Single(results, item => item.Duplicate);

        await using var verificationContext = _database.CreateContext(
            new TestCurrentTenant(seed.TenantId));

        var payment = await verificationContext.Payments
            .SingleAsync(item => item.Id == seed.Payment.Id);
        var order = await verificationContext.Orders
            .SingleAsync(item => item.Id == seed.Order.Id);
        var transactionCount = await verificationContext.PaymentTransactions
            .CountAsync(item =>
                item.PaymentIntentId == seed.Intent.Id &&
                item.ExternalEventId == "integration-event-1");

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(1, transactionCount);
    }

    private ExecutePaymentIntentHandler CreateExecuteHandler(
        MarketDbContext context,
        TenantId tenantId,
        TestProvider provider)
    {
        return new ExecutePaymentIntentHandler(
            new PaymentIntentRepository(context),
            new PaymentRepository(context),
            new TenantPaymentMethodRepository(context),
            new TenantPaymentCapabilityRepository(context),
            new PaymentStateLockRepository(context),
            new TestCurrentTenant(tenantId),
            new EfTransactionExecutor(context),
            context,
            new[] { provider },
            TimeProvider.System);
    }

    private ProcessPaymentWebhookHandler CreateWebhookHandler(
        MarketDbContext context,
        TenantId tenantId,
        TestProvider provider)
    {
        return new ProcessPaymentWebhookHandler(
            new PaymentIntentRepository(context),
            new PaymentRepository(context),
            new TenantPaymentMethodRepository(context),
            new PaymentStateLockRepository(context),
            new PaymentWebhookLockRepository(context),
            new TestCurrentTenant(tenantId),
            new EfTransactionExecutor(context),
            context,
            new[] { provider },
            TimeProvider.System);
    }

    private MarketDbContext CreateConcurrentContext(TenantId tenantId)
    {
        using var templateContext = _database.CreateContext(
            new TestCurrentTenant(tenantId));

        var connectionString = templateContext.Database.GetConnectionString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The integration test database connection string is unavailable.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = false,
            Multiplexing = false
        };

        var options = new DbContextOptionsBuilder<MarketDbContext>()
            .UseNpgsql(builder.ConnectionString)
            .Options;

        return new MarketDbContext(
            options,
            new TestCurrentTenant(tenantId));
    }

    private async Task<SeedData> SeedPaymentIntentAsync(bool markProcessing)
    {
        var now = DateTimeOffset.UtcNow;
        var tenant = Tenant.Create(
            "Provider Integration Store",
            $"provider-integration-{Guid.NewGuid():N}",
            now);

        await using (var setupContext = _database.CreateContext())
        {
            setupContext.Tenants.Add(tenant);
            await setupContext.SaveChangesAsync();
        }

        var product = Product.Create(
            tenant.Id,
            "Provider Integration Product",
            $"provider-product-{Guid.NewGuid():N}",
            Money.Create(25m, "USD"),
            now);

        await using (var context = _database.CreateContext(
                     new TestCurrentTenant(tenant.Id)))
        {
            context.Products.Add(product);
            await context.SaveChangesAsync();
        }

        var variant = ProductVariant.Create(
            tenant.Id,
            product.Id,
            "Default",
            ProductSku.Create($"PROVIDER-{Guid.NewGuid():N}"),
            CurrencyCode.Create("USD"),
            Inventory.Create(trackInventory: true, quantity: 10),
            now,
            priceOverride: Money.Create(25m, "USD"),
            isDefault: true);

        await using (var context = _database.CreateContext(
                     new TestCurrentTenant(tenant.Id)))
        {
            context.ProductVariants.Add(variant);
            await context.SaveChangesAsync();
        }

        var customerUserId = UserId.New();
        var cart = Cart.Create(
            tenant.Id,
            customerUserId,
            now,
            customerUserId.Value);

        cart.AddItem(
            product.Id,
            variant.Id,
            Money.Create(25m, "USD"),
            2,
            now,
            customerUserId.Value);

        await using (var context = _database.CreateContext(
                     new TestCurrentTenant(tenant.Id)))
        {
            context.Carts.Add(cart);
            await context.SaveChangesAsync();
        }

        var order = Order.Create(
            tenant.Id,
            customerUserId,
            cart.Id,
            CurrencyCode.Create("USD"),
            new[]
            {
                new OrderItemSnapshot(
                    product.Id,
                    variant.Id,
                    product.Name,
                    variant.Name,
                    variant.Sku.Value,
                    Money.Create(25m, "USD"),
                    2)
            },
            now,
            customerUserId.Value);

        var method = TenantPaymentMethod.Create(
            tenant.Id,
            PaymentMethodType.Electronic,
            "provider-a",
            "Provider A",
            "SY",
            "USD",
            null,
            null,
            now,
            customerUserId.Value);

        var payment = Payment.Create(
            tenant.Id,
            order.Id,
            customerUserId,
            Money.Create(50m, "USD"),
            now,
            customerUserId.Value);

        var intent = PaymentIntent.Create(
            tenant.Id,
            payment.Id,
            method.Id,
            customerUserId,
            payment.Total,
            method.Type,
            method.ProviderCode,
            now,
            customerUserId.Value);

        if (markProcessing)
        {
            intent.MarkProcessing(
                "integration-provider-reference",
                now.AddSeconds(1),
                customerUserId.Value);
        }

        await using (var context = _database.CreateContext(
                     new TestCurrentTenant(tenant.Id)))
        {
            context.Orders.Add(order);
            context.TenantPaymentMethods.Add(method);
            context.Payments.Add(payment);
            context.PaymentIntents.Add(intent);

            context.Entry(intent)
                .Property<string?>("CreateIdempotencyKey")
                .CurrentValue = $"integration-{Guid.NewGuid():N}";

            await context.SaveChangesAsync();
        }

        return new SeedData(
            tenant.Id,
            customerUserId,
            order,
            method,
            payment,
            intent);
    }

    private sealed record SeedData(
        TenantId TenantId,
        UserId CustomerUserId,
        Order Order,
        TenantPaymentMethod Method,
        Payment Payment,
        PaymentIntent Intent);

    private sealed class TestProvider :
        IPaymentProvider,
        IPaymentWebhookProvider
    {
        private int _createPaymentCallCount;
        private readonly PaymentIntentId _intentId;

        public TestProvider(PaymentIntentId intentId)
        {
            _intentId = intentId;
        }

        public PaymentProviderCode ProviderCode =>
            PaymentProviderCode.Create("provider-a");

        public int CreatePaymentCallCount =>
            Volatile.Read(ref _createPaymentCallCount);

        public PaymentWebhookResult WebhookResult { get; set; } =
            new(
                true,
                "integration-default-event",
                PaymentIntentStatus.Processing,
                "integration-provider-reference");

        public async Task<PaymentProviderResult> CreatePaymentAsync(
            PaymentProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _createPaymentCallCount);
            await Task.Delay(150, cancellationToken);

            return new PaymentProviderResult(
                PaymentIntentStatus.RequiresAction,
                "integration-provider-reference",
                new PaymentProviderAction(
                    PaymentProviderActionType.Redirect,
                    "https://payments.example.test/continue"));
        }

        public Task<PaymentWebhookResult> ParseAndValidateWebhookAsync(
            PaymentWebhookRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = WebhookResult.PaymentIntentId.HasValue
                ? WebhookResult
                : WebhookResult with { PaymentIntentId = _intentId };

            return Task.FromResult(result);
        }
    }
}
