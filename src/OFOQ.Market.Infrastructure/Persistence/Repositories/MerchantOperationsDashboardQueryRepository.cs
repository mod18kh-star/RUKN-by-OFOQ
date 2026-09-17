using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Orders;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class MerchantOperationsDashboardQueryRepository :
    IMerchantOperationsDashboardQueryRepository
{
    private readonly MarketDbContext _dbContext;

    public MerchantOperationsDashboardQueryRepository(
        MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MerchantOperationsDashboardData> GetAsync(
        DateTimeOffset nowUtc,
        TimeSpan abandonedAfter,
        int take,
        CancellationToken cancellationToken = default)
    {
        var openOrders =
            await _dbContext.Orders
                .AsNoTracking()
                .CountAsync(
                    order =>
                        order.Status != OrderStatus.Cancelled &&
                        order.Status != OrderStatus.Fulfilled,
                    cancellationToken);

        var pendingOrders =
            await _dbContext.Orders
                .AsNoTracking()
                .CountAsync(
                    order =>
                        order.Status == OrderStatus.Pending,
                    cancellationToken);

        var paidAwaitingConfirmation =
            await _dbContext.Orders
                .AsNoTracking()
                .CountAsync(
                    order =>
                        order.Status == OrderStatus.Paid,
                    cancellationToken);

        var recentOrderEntities =
            await _dbContext.Orders
                .AsNoTracking()
                .Include("_items")
                .OrderByDescending(
                    order =>
                        order.CreatedAtUtc)
                .Take(take)
                .ToListAsync(
                    cancellationToken);

        var recentOrders =
            recentOrderEntities
                .Select(
                    order =>
                        new MerchantDashboardRecentOrderData(
                            order.Id.Value,
                            order.Status.ToString(),
                            order.FulfillmentStatus.ToString(),
                            order.Currency.Value,
                            order.TotalAmount,
                            order.CreatedAtUtc))
                .ToArray();

        var lowStockRows =
            await (
                from variant in
                    _dbContext.ProductVariants
                        .AsNoTracking()
                join product in
                    _dbContext.Products
                        .AsNoTracking()
                    on variant.ProductId
                    equals product.Id
                where
                    !variant.IsDeleted &&
                    !product.IsDeleted &&
                    variant.IsEnabled &&
                    EF.Property<bool>(
                        variant,
                        "_trackInventory") &&
                    EF.Property<int>(
                        variant,
                        "_quantity") <=
                    EF.Property<int>(
                        variant,
                        "_lowStockThreshold")
                orderby
                    EF.Property<int>(
                        variant,
                        "_quantity"),
                    product.Name,
                    variant.Name
                select new
                {
                    ProductId =
                        product.Id.Value,
                    VariantId =
                        variant.Id.Value,
                    ProductName =
                        product.Name,
                    VariantName =
                        variant.Name,
                    Sku =
                        EF.Property<string>(
                            variant,
                            "_skuValue"),
                    Quantity =
                        EF.Property<int>(
                            variant,
                            "_quantity"),
                    Threshold =
                        EF.Property<int>(
                            variant,
                            "_lowStockThreshold")
                })
                .Take(take)
                .ToListAsync(
                    cancellationToken);

        var lowStockCount =
            await _dbContext.ProductVariants
                .AsNoTracking()
                .CountAsync(
                    variant =>
                        !variant.IsDeleted &&
                        variant.IsEnabled &&
                        EF.Property<bool>(
                            variant,
                            "_trackInventory") &&
                        EF.Property<int>(
                            variant,
                            "_quantity") <=
                        EF.Property<int>(
                            variant,
                            "_lowStockThreshold"),
                    cancellationToken);

        var lowStock =
            lowStockRows
                .Select(
                    row =>
                        new MerchantDashboardLowStockData(
                            row.ProductId,
                            row.VariantId,
                            row.ProductName,
                            row.VariantName,
                            row.Sku,
                            row.Quantity,
                            row.Threshold))
                .ToArray();

        var abandonedCutoff =
            nowUtc.Subtract(
                abandonedAfter);

        var activeCarts =
            await _dbContext.Carts
                .AsNoTracking()
                .Include("_items")
                .Where(
                    cart =>
                        cart.Status ==
                            CartStatus.Active &&
                        (cart.UpdatedAtUtc ??
                            cart.CreatedAtUtc) <=
                            abandonedCutoff)
                .OrderBy(
                    cart =>
                        cart.UpdatedAtUtc ??
                        cart.CreatedAtUtc)
                .ToListAsync(
                    cancellationToken);

        var abandoned =
            activeCarts
                .Where(
                    cart =>
                        cart.Items.Count > 0)
                .Select(
                    cart =>
                        new MerchantDashboardAbandonedCartData(
                            cart.Id.Value,
                            cart.CustomerUserId?
                                .Value,
                            cart.Currency?
                                .Value,
                            cart.TotalQuantity,
                            cart.TotalAmount,
                            cart.UpdatedAtUtc ??
                                cart.CreatedAtUtc))
                .ToArray();

        return new MerchantOperationsDashboardData(
            openOrders,
            pendingOrders,
            paidAwaitingConfirmation,
            lowStockCount,
            abandoned.Length,
            recentOrders,
            lowStock,
            abandoned
                .Take(take)
                .ToArray());
    }
}
