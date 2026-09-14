using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence.Repositories;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class MerchantAnalyticsQueryRepositoryTests
{
    private readonly IntegrationTestDatabase
        _database =
            IntegrationTestDatabase.Create();

    [Fact]
    public async Task GetAsync_ReturnsCapturedSalesAndTopProducts_PerCurrency()
    {
        await _database.ResetAsync();

        var now =
            new DateTimeOffset(
                2026,
                9,
                14,
                12,
                0,
                0,
                TimeSpan.Zero);

        var tenantA =
            await CreateTenantAsync(
                "Analytics Store A",
                now);

        var tenantB =
            await CreateTenantAsync(
                "Analytics Store B",
                now);

        // USD: 60 captured.
        await SeedOrderAsync(
            tenantA.Id,
            productName: "Alpha",
            currency: "USD",
            unitPrice: 30m,
            quantity: 2,
            createdAtUtc:
                now.AddDays(-10),
            paymentSucceededAtUtc:
                now.AddDays(-9),
            lifecycle:
                SeedLifecycle.Paid);

        // USD: another 40 captured.
        // Move beyond Paid to prove analytics is based
        // on PaymentStatus.Succeeded, not OrderStatus.Paid.
        await SeedOrderAsync(
            tenantA.Id,
            productName: "Beta",
            currency: "USD",
            unitPrice: 40m,
            quantity: 1,
            createdAtUtc:
                now.AddDays(-8),
            paymentSucceededAtUtc:
                now.AddDays(-7),
            lifecycle:
                SeedLifecycle.Processing);

        // SAR: 150 captured, then the order is cancelled.
        // Until refunds exist, captured sales remain captured.
        await SeedOrderAsync(
            tenantA.Id,
            productName: "Gamma",
            currency: "SAR",
            unitPrice: 50m,
            quantity: 3,
            createdAtUtc:
                now.AddDays(-6),
            paymentSucceededAtUtc:
                now.AddDays(-5),
            lifecycle:
                SeedLifecycle.Cancelled);

        // Pending order: counted as an order, but not sales.
        await SeedOrderAsync(
            tenantA.Id,
            productName: "Pending High Value",
            currency: "USD",
            unitPrice: 999m,
            quantity: 5,
            createdAtUtc:
                now.AddDays(-3),
            paymentSucceededAtUtc:
                null,
            lifecycle:
                SeedLifecycle.Pending);

        // Outside requested date range.
        await SeedOrderAsync(
            tenantA.Id,
            productName: "Old Product",
            currency: "USD",
            unitPrice: 1000m,
            quantity: 9,
            createdAtUtc:
                now.AddDays(-40),
            paymentSucceededAtUtc:
                now.AddDays(-39),
            lifecycle:
                SeedLifecycle.Paid);

        // Different tenant. Must never leak into A.
        await SeedOrderAsync(
            tenantB.Id,
            productName: "Other Tenant Product",
            currency: "USD",
            unitPrice: 5000m,
            quantity: 10,
            createdAtUtc:
                now.AddDays(-2),
            paymentSucceededAtUtc:
                now.AddDays(-1),
            lifecycle:
                SeedLifecycle.Paid);

        await using var context =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantA.Id));

        var repository =
            new MerchantAnalyticsQueryRepository(
                context);

        var result =
            await repository.GetAsync(
                now.AddDays(-30),
                now,
                topProducts: 1);

        Assert.Equal(
            4,
            result.TotalOrders);

        Assert.Equal(
            1,
            result.CancelledOrders);

        Assert.Equal(
            2,
            result.Sales.Count);

        var usd =
            Assert.Single(
                result.Sales,
                item =>
                    item.Currency ==
                    "USD");

        Assert.Equal(
            2,
            usd.PaidOrders);

        Assert.Equal(
            100m,
            usd.CapturedSales);

        Assert.Equal(
            50m,
            usd.AverageOrderValue);

        var sar =
            Assert.Single(
                result.Sales,
                item =>
                    item.Currency ==
                    "SAR");

        Assert.Equal(
            1,
            sar.PaidOrders);

        Assert.Equal(
            150m,
            sar.CapturedSales);

        Assert.Equal(
            150m,
            sar.AverageOrderValue);

        /*
         * topProducts = 1 means one top product
         * PER CURRENCY.
         */
        Assert.Equal(
            2,
            result.TopProducts.Count);

        var usdTop =
            Assert.Single(
                result.TopProducts,
                item =>
                    item.Currency ==
                    "USD");

        Assert.Equal(
            "Alpha",
            usdTop.ProductName);

        Assert.Equal(
            2,
            usdTop.QuantitySold);

        Assert.Equal(
            60m,
            usdTop.CapturedSales);

        var sarTop =
            Assert.Single(
                result.TopProducts,
                item =>
                    item.Currency ==
                    "SAR");

        Assert.Equal(
            "Gamma",
            sarTop.ProductName);

        Assert.Equal(
            3,
            sarTop.QuantitySold);

        Assert.Equal(
            150m,
            sarTop.CapturedSales);

        Assert.DoesNotContain(
            result.TopProducts,
            item =>
                item.ProductName ==
                "Pending High Value");

        Assert.DoesNotContain(
            result.TopProducts,
            item =>
                item.ProductName ==
                "Old Product");

        Assert.DoesNotContain(
            result.TopProducts,
            item =>
                item.ProductName ==
                "Other Tenant Product");
    }

    private async Task<Tenant> CreateTenantAsync(
        string name,
        DateTimeOffset now)
    {
        var tenant =
            Tenant.Create(
                name,
                $"analytics-{Guid.NewGuid():N}",
                now);

        await using var context =
            _database.CreateContext();

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        return tenant;
    }

    private async Task SeedOrderAsync(
        TenantId tenantId,
        string productName,
        string currency,
        decimal unitPrice,
        int quantity,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? paymentSucceededAtUtc,
        SeedLifecycle lifecycle)
    {
        var customerUserId =
            UserId.New();

        var product =
            Product.Create(
                tenantId,
                productName,
                $"analytics-product-{Guid.NewGuid():N}",
                Money.Create(
                    unitPrice,
                    currency),
                createdAtUtc,
                categoryId: null,
                createdByUserId:
                    customerUserId.Value);

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantId)))
        {
            context.Products.Add(
                product);

            await context.SaveChangesAsync();
        }

        var variant =
            ProductVariant.Create(
                tenantId,
                product.Id,
                "Default",
                ProductSku.Create(
                    $"AN-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    currency),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 100,
                    lowStockThreshold: 5),
                createdAtUtc,
                priceOverride:
                    Money.Create(
                        unitPrice,
                        currency),
                isDefault: true,
                createdByUserId:
                    customerUserId.Value);

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantId)))
        {
            context.ProductVariants.Add(
                variant);

            await context.SaveChangesAsync();
        }

        var cart =
            Cart.Create(
                tenantId,
                customerUserId,
                createdAtUtc,
                customerUserId.Value);

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantId)))
        {
            context.Carts.Add(
                cart);

            await context.SaveChangesAsync();
        }

        var order =
            Order.Create(
                tenantId,
                customerUserId,
                cart.Id,
                CurrencyCode.Create(
                    currency),
                new[]
                {
                    new OrderItemSnapshot(
                        product.Id,
                        variant.Id,
                        product.Name,
                        variant.Name,
                        variant.Sku.Value,
                        Money.Create(
                            unitPrice,
                            currency),
                        quantity)
                },
                createdAtUtc,
                customerUserId.Value);

        Payment? payment =
            null;

        if (paymentSucceededAtUtc.HasValue)
        {
            payment =
                Payment.Create(
                    tenantId,
                    order.Id,
                    customerUserId,
                    Money.Create(
                        order.TotalAmount,
                        currency),
                    createdAtUtc,
                    customerUserId.Value);

            payment.MarkSucceeded(
                paymentSucceededAtUtc.Value,
                customerUserId.Value);

            order.MarkPaid(
                paymentSucceededAtUtc.Value,
                customerUserId.Value);
        }

        if (lifecycle ==
            SeedLifecycle.Processing)
        {
            var paidAt =
                paymentSucceededAtUtc!.Value;

            order.Confirm(
                paidAt.AddMinutes(1),
                customerUserId.Value);

            order.StartProcessing(
                paidAt.AddMinutes(2),
                customerUserId.Value);
        }

        if (lifecycle ==
            SeedLifecycle.Cancelled)
        {
            var paidAt =
                paymentSucceededAtUtc!.Value;

            order.Cancel(
                "Analytics cancellation",
                paidAt.AddMinutes(1),
                customerUserId.Value);
        }

        await using var saveContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantId));

        saveContext.Orders.Add(
            order);

        if (payment is not null)
        {
            saveContext.Payments.Add(
                payment);
        }

        await saveContext.SaveChangesAsync();
    }

    private enum SeedLifecycle
    {
        Pending = 0,

        Paid = 1,

        Processing = 2,

        Cancelled = 3
    }
}