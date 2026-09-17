using OFOQ.Market.Application.Commerce.Analytics;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Application.Tenancy.StoreReadiness;

namespace OFOQ.Market.Application.Commerce.Dashboard;

public sealed class GetMerchantOperationsDashboardHandler
{
    public const int DefaultAbandonedAfterMinutes = 120;

    private readonly IMerchantOperationsDashboardQueryRepository _repository;
    private readonly IMerchantAnalyticsQueryRepository _analyticsRepository;
    private readonly GetStoreReadinessHandler _readinessHandler;
    private readonly ICurrentTenant _currentTenant;
    private readonly TimeProvider _timeProvider;

    public GetMerchantOperationsDashboardHandler(
        IMerchantOperationsDashboardQueryRepository repository,
        IMerchantAnalyticsQueryRepository analyticsRepository,
        GetStoreReadinessHandler readinessHandler,
        ICurrentTenant currentTenant,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _analyticsRepository = analyticsRepository;
        _readinessHandler = readinessHandler;
        _currentTenant = currentTenant;
        _timeProvider = timeProvider;
    }

    public async Task<MerchantOperationsDashboardResult> HandleAsync(
        int abandonedAfterMinutes = DefaultAbandonedAfterMinutes,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        EnsureTenant();

        if (abandonedAfterMinutes < 30 ||
            abandonedAfterMinutes > 10080)
        {
            throw new ArgumentOutOfRangeException(
                nameof(abandonedAfterMinutes),
                "Abandoned cart threshold must be between 30 minutes and 7 days.");
        }

        if (take < 1 || take > 50)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take),
                "Dashboard list size must be between 1 and 50.");
        }

        var now =
            _timeProvider.GetUtcNow();

        var readiness =
            await _readinessHandler.HandleAsync(
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Store readiness is unavailable.");

        var operations =
            await _repository.GetAsync(
                now,
                TimeSpan.FromMinutes(
                    abandonedAfterMinutes),
                take,
                cancellationToken);

        var analytics =
            await _analyticsRepository.GetAsync(
                now.AddDays(-30),
                now,
                topProducts: 5,
                cancellationToken);

        return new MerchantOperationsDashboardResult(
            new MerchantDashboardReadinessResult(
                readiness.Percentage,
                readiness.State,
                readiness.StoreStatus),
            operations.OpenOrders,
            operations.PendingOrders,
            operations.PaidOrdersAwaitingConfirmation,
            operations.LowStockVariants,
            operations.AbandonedCarts,
            abandonedAfterMinutes,
            operations.RecentOrders
                .Select(
                    x =>
                        new MerchantDashboardRecentOrderResult(
                            x.OrderId,
                            x.Status,
                            x.FulfillmentStatus,
                            x.Currency,
                            x.TotalAmount,
                            x.CreatedAtUtc))
                .ToArray(),
            operations.LowStock
                .Select(
                    x =>
                        new MerchantDashboardLowStockResult(
                            x.ProductId,
                            x.VariantId,
                            x.ProductName,
                            x.VariantName,
                            x.Sku,
                            x.Quantity,
                            x.LowStockThreshold))
                .ToArray(),
            operations.AbandonedCartItems
                .Select(
                    x =>
                        new MerchantDashboardAbandonedCartResult(
                            x.CartId,
                            x.CustomerUserId,
                            x.Currency,
                            x.TotalQuantity,
                            x.TotalAmount,
                            x.LastActivityAtUtc))
                .ToArray(),
            analytics.TopProducts
                .Select(
                    x =>
                        new MerchantDashboardTopProductResult(
                            x.ProductId,
                            x.ProductName,
                            x.Currency,
                            x.QuantitySold,
                            x.CapturedSales))
                .ToArray());
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "Tenant context is required.");
        }
    }
}
