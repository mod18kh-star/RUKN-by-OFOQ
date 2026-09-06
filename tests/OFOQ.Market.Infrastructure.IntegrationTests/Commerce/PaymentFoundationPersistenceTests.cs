using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class PaymentFoundationPersistenceTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task PaymentFoundation_RoundTrip_PreservesPaymentIntentAndTransaction()
    {
        await _database.ResetAsync();

        var seed = await SeedOrderAsync("Payment Store A");
        var now = DateTimeOffset.UtcNow;

        var method = TenantPaymentMethod.Create(
            seed.TenantId,
            PaymentMethodType.Electronic,
            "sham-cash",
            "Sham Cash",
            "SY",
            "USD",
            null,
            null,
            now,
            seed.CustomerUserId.Value);

        var capability = TenantPaymentCapability.Create(
            seed.TenantId,
            now,
            seed.CustomerUserId.Value);

        var payment = Payment.Create(
            seed.TenantId,
            seed.Order.Id,
            seed.CustomerUserId,
            Money.Create(seed.Order.TotalAmount, seed.Order.Currency),
            now,
            seed.CustomerUserId.Value);

        var intent = PaymentIntent.Create(
            seed.TenantId,
            payment.Id,
            method.Id,
            seed.CustomerUserId,
            payment.Total,
            method.Type,
            method.ProviderCode,
            now,
            seed.CustomerUserId.Value);

        intent.MarkProcessing(
            "provider-ref-1",
            now.AddSeconds(1),
            seed.CustomerUserId.Value);

        intent.RecordTransaction(
            PaymentTransactionType.ProviderRequest,
            "provider-ref-1",
            "external-event-1",
            now.AddSeconds(2));

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(seed.TenantId)))
        {
            context.TenantPaymentMethods.Add(method);
            context.TenantPaymentCapabilities.Add(capability);
            context.Payments.Add(payment);

            var intentEntry = context.PaymentIntents.Add(intent);
            intentEntry.Property<string?>("CreateIdempotencyKey")
                .CurrentValue = "payment-key-1";

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(seed.TenantId));

        var savedPayment = await verificationContext.Payments
            .SingleAsync(item => item.Id == payment.Id);

        var savedIntent = await verificationContext.PaymentIntents
            .Include("_transactions")
            .SingleAsync(item => item.Id == intent.Id);

        var savedMethod = await verificationContext.TenantPaymentMethods
            .SingleAsync(item => item.Id == method.Id);

        var savedCapability = await verificationContext.TenantPaymentCapabilities
            .SingleAsync();

        Assert.Equal(seed.Order.Id, savedPayment.OrderId);
        Assert.Equal(PaymentStatus.Pending, savedPayment.Status);
        Assert.Equal(seed.Order.TotalAmount, savedPayment.Amount);
        Assert.Equal("USD", savedPayment.Currency.Value);

        Assert.Equal(PaymentIntentStatus.Processing, savedIntent.Status);
        Assert.Equal("sham-cash", savedIntent.ProviderCode.Value);
        Assert.Equal("provider-ref-1", savedIntent.ProviderReference);
        Assert.Single(savedIntent.Transactions);

        Assert.Equal("SY", savedMethod.Country.Value);
        Assert.Equal("USD", savedMethod.Currency.Value);
        Assert.True(savedMethod.IsEnabled);
        Assert.True(savedCapability.ElectronicPaymentsAllowed);
    }

    [Fact]
    public async Task Database_RejectsSecondPaymentForSameOrder()
    {
        await _database.ResetAsync();

        var seed = await SeedOrderAsync("Payment Store B");
        var now = DateTimeOffset.UtcNow;

        var first = Payment.Create(
            seed.TenantId,
            seed.Order.Id,
            seed.CustomerUserId,
            Money.Create(seed.Order.TotalAmount, seed.Order.Currency),
            now);

        var second = Payment.Create(
            seed.TenantId,
            seed.Order.Id,
            seed.CustomerUserId,
            Money.Create(seed.Order.TotalAmount, seed.Order.Currency),
            now.AddSeconds(1));

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(seed.TenantId)))
        {
            context.Payments.Add(first);
            await context.SaveChangesAsync();
        }

        await using var duplicateContext =
            _database.CreateContext(
                new TestCurrentTenant(seed.TenantId));

        duplicateContext.Payments.Add(second);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => duplicateContext.SaveChangesAsync());

        var postgresException = Assert.IsType<PostgresException>(
            exception.InnerException);

        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
        Assert.Equal(
            "ux_commerce_payments_tenant_order",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_RejectsDuplicatePaymentIntentIdempotencyKeyForSameCustomer()
    {
        await _database.ResetAsync();

        var seed = await SeedOrderAsync("Payment Store C");
        var now = DateTimeOffset.UtcNow;

        var method = TenantPaymentMethod.Create(
            seed.TenantId,
            PaymentMethodType.Electronic,
            "provider-a",
            "Provider A",
            "SY",
            "USD",
            null,
            null,
            now);

        var payment = Payment.Create(
            seed.TenantId,
            seed.Order.Id,
            seed.CustomerUserId,
            Money.Create(seed.Order.TotalAmount, seed.Order.Currency),
            now);

        var firstIntent = PaymentIntent.Create(
            seed.TenantId,
            payment.Id,
            method.Id,
            seed.CustomerUserId,
            payment.Total,
            method.Type,
            method.ProviderCode,
            now);

        var secondIntent = PaymentIntent.Create(
            seed.TenantId,
            payment.Id,
            method.Id,
            seed.CustomerUserId,
            payment.Total,
            method.Type,
            method.ProviderCode,
            now.AddSeconds(1));

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(seed.TenantId)))
        {
            context.TenantPaymentMethods.Add(method);
            context.Payments.Add(payment);

            var firstEntry = context.PaymentIntents.Add(firstIntent);
            firstEntry.Property<string?>("CreateIdempotencyKey")
                .CurrentValue = "same-key";

            await context.SaveChangesAsync();
        }

        await using var duplicateContext =
            _database.CreateContext(
                new TestCurrentTenant(seed.TenantId));

        var secondEntry = duplicateContext.PaymentIntents.Add(secondIntent);
        secondEntry.Property<string?>("CreateIdempotencyKey")
            .CurrentValue = "same-key";

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => duplicateContext.SaveChangesAsync());

        var postgresException = Assert.IsType<PostgresException>(
            exception.InnerException);

        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
        Assert.Equal(
            "ux_commerce_payment_intents_create_idempotency",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task PaymentQueries_AreTenantIsolated()
    {
        await _database.ResetAsync();

        var seedA = await SeedOrderAsync("Payment Tenant A");
        var seedB = await SeedOrderAsync("Payment Tenant B");
        var now = DateTimeOffset.UtcNow;

        var paymentA = Payment.Create(
            seedA.TenantId,
            seedA.Order.Id,
            seedA.CustomerUserId,
            Money.Create(seedA.Order.TotalAmount, seedA.Order.Currency),
            now);

        var paymentB = Payment.Create(
            seedB.TenantId,
            seedB.Order.Id,
            seedB.CustomerUserId,
            Money.Create(seedB.Order.TotalAmount, seedB.Order.Currency),
            now);

        await using (var contextA =
                     _database.CreateContext(
                         new TestCurrentTenant(seedA.TenantId)))
        {
            contextA.Payments.Add(paymentA);
            await contextA.SaveChangesAsync();
        }

        await using (var contextB =
                     _database.CreateContext(
                         new TestCurrentTenant(seedB.TenantId)))
        {
            contextB.Payments.Add(paymentB);
            await contextB.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(seedA.TenantId));

        var payments = await verificationContext.Payments.ToListAsync();

        var saved = Assert.Single(payments);
        Assert.Equal(paymentA.Id, saved.Id);
        Assert.Equal(seedA.TenantId, saved.TenantId);
    }

    private async Task<SeededOrderData> SeedOrderAsync(string tenantName)
    {
        var now = DateTimeOffset.UtcNow;

        var tenant = Tenant.Create(
            tenantName,
            $"payment-store-{Guid.NewGuid():N}",
            now);

        await using (var setupContext = _database.CreateContext())
        {
            setupContext.Tenants.Add(tenant);
            await setupContext.SaveChangesAsync();
        }

        var product = Product.Create(
            tenant.Id,
            "Payment Product",
            $"payment-product-{Guid.NewGuid():N}",
            Money.Create(30m, "USD"),
            now);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(tenant.Id)))
        {
            tenantContext.Products.Add(product);
            await tenantContext.SaveChangesAsync();
        }

        var variant = ProductVariant.Create(
            tenant.Id,
            product.Id,
            "Default",
            ProductSku.Create($"PAY-{Guid.NewGuid():N}"),
            CurrencyCode.Create("USD"),
            Inventory.Create(
                trackInventory: true,
                quantity: 10,
                lowStockThreshold: 2),
            now,
            priceOverride: Money.Create(30m, "USD"),
            isDefault: true);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(tenant.Id)))
        {
            tenantContext.ProductVariants.Add(variant);
            await tenantContext.SaveChangesAsync();
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
            Money.Create(30m, "USD"),
            2,
            now,
            customerUserId.Value);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(tenant.Id)))
        {
            tenantContext.Carts.Add(cart);
            await tenantContext.SaveChangesAsync();
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
                    Money.Create(30m, "USD"),
                    2)
            },
            now,
            customerUserId.Value);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(tenant.Id)))
        {
            tenantContext.Orders.Add(order);
            await tenantContext.SaveChangesAsync();
        }

        return new SeededOrderData(
            tenant.Id,
            customerUserId,
            order);
    }

    private sealed record SeededOrderData(
        TenantId TenantId,
        UserId CustomerUserId,
        Order Order);
}
