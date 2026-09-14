using OFOQ.Market.Application.Commerce.Analytics;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryMerchantAnalyticsQueryRepository :
    IMerchantAnalyticsQueryRepository
{
    private readonly InMemoryOrderStore
        _orderStore;

    private readonly InMemoryPaymentStore
        _paymentStore;

    private readonly ICurrentTenant
        _currentTenant;

    public InMemoryMerchantAnalyticsQueryRepository(
        InMemoryOrderStore orderStore,
        InMemoryPaymentStore paymentStore,
        ICurrentTenant currentTenant)
    {
        _orderStore =
            orderStore;

        _paymentStore =
            paymentStore;

        _currentTenant =
            currentTenant;
    }

    public Task<MerchantAnalyticsResult> GetAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int topProducts,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            GetRequiredTenantId();

        Order[] orders;

        lock (_orderStore.SyncRoot)
        {
            orders =
                _orderStore.Items
                    .Select(
                        entry =>
                            entry.Order)
                    .Where(
                        order =>
                            order.TenantId ==
                            tenantId)
                    .ToArray();
        }

        Payment[] payments;

        lock (_paymentStore.SyncRoot)
        {
            payments =
                _paymentStore.Items
                    .Where(
                        payment =>
                            payment.TenantId ==
                            tenantId)
                    .ToArray();
        }

        var totalOrders =
            orders.Count(
                order =>
                    order.CreatedAtUtc >=
                        fromUtc &&
                    order.CreatedAtUtc <
                        toUtc);

        var cancelledOrders =
            orders.Count(
                order =>
                    order.CreatedAtUtc >=
                        fromUtc &&
                    order.CreatedAtUtc <
                        toUtc &&
                    order.Status ==
                        OrderStatus.Cancelled);

        var succeededPayments =
            payments
                .Where(
                    payment =>
                        payment.Status ==
                        PaymentStatus.Succeeded)
                .Where(
                    payment =>
                    {
                        var capturedAt =
                            payment.UpdatedAtUtc
                            ?? payment.CreatedAtUtc;

                        return
                            capturedAt >= fromUtc &&
                            capturedAt < toUtc;
                    })
                .ToArray();

        var sales =
            succeededPayments
                .GroupBy(
                    payment =>
                        payment.Currency.Value,
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
                                payment =>
                                    payment.Amount);

                        var paidOrders =
                            group.Count();

                        return new MerchantAnalyticsCurrencyResult(
                            group.Key,
                            paidOrders,
                            capturedSales,
                            capturedSales /
                            paidOrders);
                    })
                .ToArray();

        var ordersById =
            orders.ToDictionary(
                order =>
                    order.Id);

        var productTotals =
            succeededPayments
                .Where(
                    payment =>
                        ordersById.ContainsKey(
                            payment.OrderId))
                .SelectMany(
                    payment =>
                        ordersById[
                                payment.OrderId]
                            .Items)
                .GroupBy(
                    item =>
                        new
                        {
                            item.ProductId,
                            item.ProductName,

                            Currency =
                                item.UnitPrice.Currency.Value
                        })
                .Select(
                    group =>
                        new MerchantAnalyticsTopProductResult(
                            group.Key.ProductId.Value,
                            group.Key.ProductName,
                            group.Key.Currency,
                            group.Sum(
                                item =>
                                    item.Quantity),
                            group.Sum(
                                item =>
                                    item.LineTotal)))
                .ToArray();

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

        return Task.FromResult(
            new MerchantAnalyticsResult(
                fromUtc,
                toUtc,
                totalOrders,
                cancelledOrders,
                sales,
                top));
    }

    private TenantId GetRequiredTenantId()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required.");
        }

        return _currentTenant.TenantId.Value;
    }
}