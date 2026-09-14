using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Commerce.Analytics;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class MerchantAnalyticsQueryRepository :
    IMerchantAnalyticsQueryRepository
{
    private readonly MarketDbContext
        _dbContext;

    public MerchantAnalyticsQueryRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<MerchantAnalyticsResult> GetAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int topProducts,
        CancellationToken cancellationToken = default)
    {
        var totalOrders =
            await _dbContext
                .Orders
                .AsNoTracking()
                .CountAsync(
                    order =>
                        order.CreatedAtUtc >= fromUtc &&
                        order.CreatedAtUtc < toUtc,
                    cancellationToken);

        var cancelledOrders =
            await _dbContext
                .Orders
                .AsNoTracking()
                .CountAsync(
                    order =>
                        order.CreatedAtUtc >= fromUtc &&
                        order.CreatedAtUtc < toUtc &&
                        order.Status ==
                            OrderStatus.Cancelled,
                    cancellationToken);

        /*
         * Keep SQL deliberately simple:
         * PostgreSQL filters the relevant succeeded
         * payments for the requested date range.
         *
         * Aggregation is done after materialization.
         * This avoids fragile translation of GroupBy
         * over strongly typed IDs and field-mapped
         * currency properties.
         */
        var paymentRows =
            await _dbContext
                .Payments
                .AsNoTracking()
                .Where(
                    payment =>
                        payment.Status ==
                        PaymentStatus.Succeeded)
                .Where(
                    payment =>
                        (payment.UpdatedAtUtc
                            ?? payment.CreatedAtUtc) >=
                            fromUtc &&
                        (payment.UpdatedAtUtc
                            ?? payment.CreatedAtUtc) <
                            toUtc)
                .Select(
                    payment =>
                        new
                        {
                            payment.OrderId,
                            payment.Amount,

                            Currency =
                                EF.Property<string>(
                                    payment,
                                    "_currencyCode")
                        })
                .ToListAsync(
                    cancellationToken);

        var sales =
            paymentRows
                .GroupBy(
                    row =>
                        row.Currency,
                    StringComparer.Ordinal)
                .OrderBy(
                    group =>
                        group.Key,
                    StringComparer.Ordinal)
                .Select(
                    group =>
                    {
                        var capturedSales =
                            group.Sum(
                                row =>
                                    row.Amount);

                        var paidOrders =
                            group.Count();

                        return new MerchantAnalyticsCurrencyResult(
                            group.Key,
                            paidOrders,
                            capturedSales,
                            paidOrders == 0
                                ? 0m
                                : capturedSales /
                                  paidOrders);
                    })
                .ToArray();

        /*
         * Fetch only order-item rows belonging to
         * succeeded payments in the requested range.
         *
         * The join stays in PostgreSQL; ranking and
         * aggregation happen in memory afterward.
         */
        var saleItemRows =
            await (
                from payment in
                    _dbContext
                        .Payments
                        .AsNoTracking()

                where
                    payment.Status ==
                        PaymentStatus.Succeeded &&
                    (payment.UpdatedAtUtc
                        ?? payment.CreatedAtUtc) >=
                        fromUtc &&
                    (payment.UpdatedAtUtc
                        ?? payment.CreatedAtUtc) <
                        toUtc

                join item in
                    _dbContext
                        .OrderItems
                        .AsNoTracking()

                    on payment.OrderId
                    equals item.OrderId

                select new
                {
                    item.ProductId,

                    item.ProductName,

                    item.Quantity,

                    UnitPrice =
                        EF.Property<decimal>(
                            item,
                            "_unitPriceAmount"),

                    Currency =
                        EF.Property<string>(
                            item,
                            "_unitPriceCurrencyCode")
                })
                .ToListAsync(
                    cancellationToken);

        var productTotals =
            saleItemRows
                .GroupBy(
                    row =>
                        new
                        {
                            row.ProductId,
                            row.ProductName,
                            row.Currency
                        })
                .Select(
                    group =>
                        new MerchantAnalyticsTopProductResult(
                            group.Key.ProductId.Value,
                            group.Key.ProductName,
                            group.Key.Currency,
                            group.Sum(
                                row =>
                                    row.Quantity),
                            group.Sum(
                                row =>
                                    row.UnitPrice *
                                    row.Quantity)))
                .ToArray();

        /*
         * topProducts means top N PER currency.
         * Revenue from different currencies must never
         * be ranked against each other.
         */
        var top =
            productTotals
                .GroupBy(
                    result =>
                        result.Currency,
                    StringComparer.Ordinal)
                .OrderBy(
                    group =>
                        group.Key,
                    StringComparer.Ordinal)
                .SelectMany(
                    group =>
                        group
                            .OrderByDescending(
                                result =>
                                    result.CapturedSales)
                            .ThenByDescending(
                                result =>
                                    result.QuantitySold)
                            .ThenBy(
                                result =>
                                    result.ProductName,
                                StringComparer.Ordinal)
                            .Take(
                                topProducts))
                .ToArray();

        return new MerchantAnalyticsResult(
            fromUtc,
            toUtc,
            totalOrders,
            cancelledOrders,
            sales,
            top);
    }
}