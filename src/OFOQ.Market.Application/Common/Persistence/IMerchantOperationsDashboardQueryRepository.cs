namespace OFOQ.Market.Application.Common.Persistence;

public sealed record MerchantDashboardRecentOrderData(
    Guid OrderId,
    string Status,
    string FulfillmentStatus,
    string Currency,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc);

public sealed record MerchantDashboardLowStockData(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string VariantName,
    string Sku,
    int Quantity,
    int LowStockThreshold);

public sealed record MerchantDashboardAbandonedCartData(
    Guid CartId,
    Guid? CustomerUserId,
    string? Currency,
    int TotalQuantity,
    decimal TotalAmount,
    DateTimeOffset LastActivityAtUtc);

public sealed record MerchantOperationsDashboardData(
    int OpenOrders,
    int PendingOrders,
    int PaidOrdersAwaitingConfirmation,
    int LowStockVariants,
    int AbandonedCarts,
    IReadOnlyList<MerchantDashboardRecentOrderData> RecentOrders,
    IReadOnlyList<MerchantDashboardLowStockData> LowStock,
    IReadOnlyList<MerchantDashboardAbandonedCartData> AbandonedCartItems);

public interface IMerchantOperationsDashboardQueryRepository
{
    Task<MerchantOperationsDashboardData> GetAsync(
        DateTimeOffset nowUtc,
        TimeSpan abandonedAfter,
        int take,
        CancellationToken cancellationToken = default);
}
