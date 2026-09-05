using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class CartManagementPersistenceTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task UpdateQuantity_PersistsAfterReload()
    {
        await _database.ResetAsync();

        var seed =
            await SeedCartAsync(
                includeSecondItem: false);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            var cart =
                await tenantContext
                    .Carts
                    .Include("_items")
                    .SingleAsync(
                        item =>
                            item.Id ==
                            seed.CartId);

            var item =
                Assert.Single(
                    cart.Items);

            Assert.Equal(
                seed.FirstItemId,
                item.Id);

            cart.ChangeItemQuantity(
                item.Id,
                4,
                DateTimeOffset.UtcNow,
                seed.CustomerUserId.Value);

            await tenantContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var saved =
            await verificationContext
                .Carts
                .Include("_items")
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.CartId);

        var savedItem =
            Assert.Single(
                saved.Items);

        Assert.Equal(
            seed.FirstItemId,
            savedItem.Id);

        Assert.Equal(
            4,
            savedItem.Quantity);

        Assert.Equal(
            4,
            saved.TotalQuantity);

        Assert.Equal(
            40m,
            saved.TotalAmount);

        Assert.NotNull(
            saved.Currency);

        Assert.Equal(
            "USD",
            saved.Currency!.Value.Value);
    }

    [Fact]
    public async Task RemoveItem_PersistsDeletionAfterReload()
    {
        await _database.ResetAsync();

        var seed =
            await SeedCartAsync(
                includeSecondItem: true);

        Assert.True(
            seed.SecondItemId.HasValue);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            var cart =
                await tenantContext
                    .Carts
                    .Include("_items")
                    .SingleAsync(
                        item =>
                            item.Id ==
                            seed.CartId);

            Assert.Equal(
                2,
                cart.Items.Count);

            cart.RemoveItem(
                seed.FirstItemId,
                DateTimeOffset.UtcNow,
                seed.CustomerUserId.Value);

            await tenantContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var saved =
            await verificationContext
                .Carts
                .Include("_items")
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.CartId);

        var remainingItem =
            Assert.Single(
                saved.Items);

        Assert.Equal(
            seed.SecondItemId!.Value,
            remainingItem.Id);

        Assert.Equal(
            2,
            remainingItem.Quantity);

        Assert.Equal(
            2,
            saved.TotalQuantity);

        Assert.Equal(
            40m,
            saved.TotalAmount);

        Assert.NotNull(
            saved.Currency);

        Assert.Equal(
            "USD",
            saved.Currency!.Value.Value);

        var persistedItemCount =
            await verificationContext
                .CartItems
                .CountAsync(
                    item =>
                        item.CartId ==
                        seed.CartId);

        Assert.Equal(
            1,
            persistedItemCount);
    }

    [Fact]
    public async Task RemoveLastItem_PersistsCurrencyResetAfterReload()
    {
        await _database.ResetAsync();

        var seed =
            await SeedCartAsync(
                includeSecondItem: false);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            var cart =
                await tenantContext
                    .Carts
                    .Include("_items")
                    .SingleAsync(
                        item =>
                            item.Id ==
                            seed.CartId);

            var item =
                Assert.Single(
                    cart.Items);

            cart.RemoveItem(
                item.Id,
                DateTimeOffset.UtcNow,
                seed.CustomerUserId.Value);

            await tenantContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var saved =
            await verificationContext
                .Carts
                .Include("_items")
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.CartId);

        Assert.Empty(
            saved.Items);

        Assert.Equal(
            0,
            saved.TotalQuantity);

        Assert.Equal(
            0m,
            saved.TotalAmount);

        Assert.Null(
            saved.Currency);

        var persistedItemCount =
            await verificationContext
                .CartItems
                .CountAsync(
                    item =>
                        item.CartId ==
                        seed.CartId);

        Assert.Equal(
            0,
            persistedItemCount);
    }

    [Fact]
    public async Task ClearCart_PersistsAllItemDeletionsAndCurrencyReset()
    {
        await _database.ResetAsync();

        var seed =
            await SeedCartAsync(
                includeSecondItem: true);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            var cart =
                await tenantContext
                    .Carts
                    .Include("_items")
                    .SingleAsync(
                        item =>
                            item.Id ==
                            seed.CartId);

            Assert.Equal(
                2,
                cart.Items.Count);

            Assert.Equal(
                3,
                cart.TotalQuantity);

            Assert.Equal(
                50m,
                cart.TotalAmount);

            cart.Clear(
                DateTimeOffset.UtcNow,
                seed.CustomerUserId.Value);

            await tenantContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var saved =
            await verificationContext
                .Carts
                .Include("_items")
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.CartId);

        Assert.Empty(
            saved.Items);

        Assert.Equal(
            0,
            saved.TotalQuantity);

        Assert.Equal(
            0m,
            saved.TotalAmount);

        Assert.Null(
            saved.Currency);

        Assert.Equal(
            CartStatus.Active,
            saved.Status);

        var persistedItemCount =
            await verificationContext
                .CartItems
                .CountAsync(
                    item =>
                        item.CartId ==
                        seed.CartId);

        Assert.Equal(
            0,
            persistedItemCount);
    }

    [Fact]
    public async Task CrossTenantCartMutation_IsRejectedBySaveChanges()
    {
        await _database.ResetAsync();

        var seed =
            await SeedCartAsync(
                includeSecondItem: false);

        var tenantA =
            Tenant.Create(
                "Tenant A",
                $"tenant-a-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.Add(
                tenantA);

            await setupContext.SaveChangesAsync();
        }

        Cart detachedCart;

        await using (var tenantBContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            detachedCart =
                await tenantBContext
                    .Carts
                    .Include("_items")
                    .SingleAsync(
                        item =>
                            item.Id ==
                            seed.CartId);
        }

        var detachedItem =
            Assert.Single(
                detachedCart.Items);

        await using (var tenantAContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantA.Id)))
        {
            tenantAContext.Carts.Attach(
                detachedCart);

            detachedCart.ChangeItemQuantity(
                detachedItem.Id,
                3,
                DateTimeOffset.UtcNow,
                seed.CustomerUserId.Value);

            await Assert.ThrowsAsync<
                TenantScopeViolationException>(
                    () =>
                        tenantAContext
                            .SaveChangesAsync());
        }

        /*
         * Verify the rejected cross-tenant attempt
         * did not modify the real Tenant B row.
         */
        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var saved =
            await verificationContext
                .Carts
                .Include("_items")
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.CartId);

        var savedItem =
            Assert.Single(
                saved.Items);

        Assert.Equal(
            1,
            savedItem.Quantity);

        Assert.Equal(
            10m,
            saved.TotalAmount);
    }

    [Fact]
    public async Task ConvertedCart_IsNotReturnedAsActiveCart()
    {
        await _database.ResetAsync();

        var seed =
            await SeedCartAsync(
                includeSecondItem: false);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             seed.TenantId)))
        {
            var cart =
                await tenantContext
                    .Carts
                    .Include("_items")
                    .SingleAsync(
                        item =>
                            item.Id ==
                            seed.CartId);

            cart.MarkConverted(
                DateTimeOffset.UtcNow);

            await tenantContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    seed.TenantId));

        var convertedCart =
            await verificationContext
                .Carts
                .SingleAsync(
                    item =>
                        item.Id ==
                        seed.CartId);

        Assert.Equal(
            CartStatus.Converted,
            convertedCart.Status);

        var activeCart =
            await verificationContext
                .Carts
                .SingleOrDefaultAsync(
                    item =>
                        item.Status ==
                            CartStatus.Active &&
                        EF.Property<Guid?>(
                            item,
                            "_customerUserId") ==
                        seed.CustomerUserId.Value);

        Assert.Null(
            activeCart);
    }

    private async Task<SeededCart> SeedCartAsync(
        bool includeSecondItem)
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Persistence Store",
                $"persistence-store-{Guid.NewGuid():N}",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.Add(
                tenant);

            await setupContext.SaveChangesAsync();
        }

        var firstProduct =
            Product.Create(
                tenant.Id,
                "Product A",
                $"product-a-{Guid.NewGuid():N}",
                Money.Create(
                    10m,
                    "USD"),
                now);

        var secondProduct =
            Product.Create(
                tenant.Id,
                "Product B",
                $"product-b-{Guid.NewGuid():N}",
                Money.Create(
                    20m,
                    "USD"),
                now);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Products.Add(
                firstProduct);

            if (includeSecondItem)
            {
                tenantContext.Products.Add(
                    secondProduct);
            }

            await tenantContext.SaveChangesAsync();
        }

        var firstVariant =
            ProductVariant.Create(
                tenant.Id,
                firstProduct.Id,
                "Default",
                ProductSku.Create(
                    $"FIRST-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 100),
                now,
                isDefault:
                    true);

        var secondVariant =
            ProductVariant.Create(
                tenant.Id,
                secondProduct.Id,
                "Default",
                ProductSku.Create(
                    $"SECOND-{Guid.NewGuid():N}"),
                CurrencyCode.Create(
                    "USD"),
                Inventory.Create(
                    trackInventory: true,
                    quantity: 100),
                now,
                isDefault:
                    true);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.ProductVariants.Add(
                firstVariant);

            if (includeSecondItem)
            {
                tenantContext.ProductVariants.Add(
                    secondVariant);
            }

            await tenantContext.SaveChangesAsync();
        }

        var customerUserId =
            UserId.New();

        var cart =
            Cart.Create(
                tenant.Id,
                customerUserId,
                now);

        cart.AddItem(
            firstProduct.Id,
            firstVariant.Id,
            Money.Create(
                10m,
                "USD"),
            1,
            now);

        if (includeSecondItem)
        {
            cart.AddItem(
                secondProduct.Id,
                secondVariant.Id,
                Money.Create(
                    20m,
                    "USD"),
                2,
                now);
        }

        var firstItemId =
            cart.Items
                .Single(
                    item =>
                        item.ProductVariantId ==
                        firstVariant.Id)
                .Id;

        CartItemId? secondItemId =
            null;

        if (includeSecondItem)
        {
            secondItemId =
                cart.Items
                    .Single(
                        item =>
                            item.ProductVariantId ==
                            secondVariant.Id)
                    .Id;
        }

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Carts.Add(
                cart);

            await tenantContext.SaveChangesAsync();
        }

        return new SeededCart(
            tenant.Id,
            customerUserId,
            cart.Id,
            firstItemId,
            secondItemId);
    }

    private sealed record SeededCart(
        TenantId TenantId,
        UserId CustomerUserId,
        CartId CartId,
        CartItemId FirstItemId,
        CartItemId? SecondItemId);
}