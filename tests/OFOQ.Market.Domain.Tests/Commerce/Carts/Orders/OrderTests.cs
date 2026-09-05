using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Commerce.Orders;

public sealed class OrderTests
{
    [Fact]
    public void Create_PreservesCheckoutSnapshotAndTotals()
    {
        var tenantId =
            TenantId.New();

        var customerUserId =
            UserId.New();

        var sourceCartId =
            CartId.New();

        var productAId =
            ProductId.New();

        var productBId =
            ProductId.New();

        var variantAId =
            ProductVariantId.New();

        var variantBId =
            ProductVariantId.New();

        var now =
            DateTimeOffset.UtcNow;

        var order =
            Order.Create(
                tenantId,
                customerUserId,
                sourceCartId,
                CurrencyCode.Create(
                    "USD"),
                new[]
                {
                    new OrderItemSnapshot(
                        productAId,
                        variantAId,
                        "T-Shirt",
                        "Black / M",
                        "SHIRT-BLACK-M",
                        Money.Create(
                            25m,
                            "USD"),
                        2),

                    new OrderItemSnapshot(
                        productBId,
                        variantBId,
                        "Shoes",
                        "42",
                        "SHOES-42",
                        Money.Create(
                            50m,
                            "USD"),
                        1)
                },
                now,
                customerUserId.Value);

        Assert.Equal(
            tenantId,
            order.TenantId);

        Assert.Equal(
            customerUserId,
            order.CustomerUserId);

        Assert.Equal(
            sourceCartId,
            order.SourceCartId);

        Assert.Equal(
            OrderStatus.Pending,
            order.Status);

        Assert.Equal(
            "USD",
            order.Currency.Value);

        Assert.Equal(
            3,
            order.TotalQuantity);

        Assert.Equal(
            100m,
            order.TotalAmount);

        Assert.Equal(
            2,
            order.Items.Count);

        var firstItem =
            order.Items.Single(
                item =>
                    item.ProductVariantId ==
                    variantAId);

        Assert.Equal(
            "T-Shirt",
            firstItem.ProductName);

        Assert.Equal(
            "Black / M",
            firstItem.VariantName);

        Assert.Equal(
            "SHIRT-BLACK-M",
            firstItem.Sku);

        Assert.Equal(
            25m,
            firstItem.UnitPrice.Amount);

        Assert.Equal(
            2,
            firstItem.Quantity);

        Assert.Equal(
            50m,
            firstItem.LineTotal);
    }

    [Fact]
    public void Create_WithNoItems_IsRejected()
    {
        var exception =
            Assert.Throws<
                ArgumentException>(
                    () =>
                        Order.Create(
                            TenantId.New(),
                            UserId.New(),
                            CartId.New(),
                            CurrencyCode.Create(
                                "USD"),
                            Array.Empty<
                                OrderItemSnapshot>(),
                            DateTimeOffset.UtcNow));

        Assert.Contains(
            "at least one item",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithMixedCurrencies_IsRejected()
    {
        var productAId =
            ProductId.New();

        var productBId =
            ProductId.New();

        var exception =
            Assert.Throws<
                ArgumentException>(
                    () =>
                        Order.Create(
                            TenantId.New(),
                            UserId.New(),
                            CartId.New(),
                            CurrencyCode.Create(
                                "USD"),
                            new[]
                            {
                                new OrderItemSnapshot(
                                    productAId,
                                    ProductVariantId.New(),
                                    "Product A",
                                    "Default",
                                    "A-1",
                                    Money.Create(
                                        10m,
                                        "USD"),
                                    1),

                                new OrderItemSnapshot(
                                    productBId,
                                    ProductVariantId.New(),
                                    "Product B",
                                    "Default",
                                    "B-1",
                                    Money.Create(
                                        20m,
                                        "EUR"),
                                    1)
                            },
                            DateTimeOffset.UtcNow));

        Assert.Contains(
            "order currency",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithDuplicateVariant_IsRejected()
    {
        var variantId =
            ProductVariantId.New();

        var exception =
            Assert.Throws<
                ArgumentException>(
                    () =>
                        Order.Create(
                            TenantId.New(),
                            UserId.New(),
                            CartId.New(),
                            CurrencyCode.Create(
                                "USD"),
                            new[]
                            {
                                new OrderItemSnapshot(
                                    ProductId.New(),
                                    variantId,
                                    "Product",
                                    "Default",
                                    "SKU-1",
                                    Money.Create(
                                        10m,
                                        "USD"),
                                    1),

                                new OrderItemSnapshot(
                                    ProductId.New(),
                                    variantId,
                                    "Product Duplicate",
                                    "Default",
                                    "SKU-1",
                                    Money.Create(
                                        10m,
                                        "USD"),
                                    1)
                            },
                            DateTimeOffset.UtcNow));

        Assert.Contains(
            "same product variant",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithInvalidQuantity_IsRejected()
    {
        Assert.Throws<
            ArgumentOutOfRangeException>(
                () =>
                    Order.Create(
                        TenantId.New(),
                        UserId.New(),
                        CartId.New(),
                        CurrencyCode.Create(
                            "USD"),
                        new[]
                        {
                            new OrderItemSnapshot(
                                ProductId.New(),
                                ProductVariantId.New(),
                                "Product",
                                "Default",
                                "SKU-1",
                                Money.Create(
                                    10m,
                                    "USD"),
                                0)
                        },
                        DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_NormalizesSnapshotText()
    {
        var order =
            Order.Create(
                TenantId.New(),
                UserId.New(),
                CartId.New(),
                CurrencyCode.Create(
                    "USD"),
                new[]
                {
                    new OrderItemSnapshot(
                        ProductId.New(),
                        ProductVariantId.New(),
                        "  Product Name  ",
                        "  Black / Large  ",
                        "  SKU-123  ",
                        Money.Create(
                            10m,
                            "USD"),
                        1)
                },
                DateTimeOffset.UtcNow);

        var item =
            Assert.Single(
                order.Items);

        Assert.Equal(
            "Product Name",
            item.ProductName);

        Assert.Equal(
            "Black / Large",
            item.VariantName);

        Assert.Equal(
            "SKU-123",
            item.Sku);
    }
}