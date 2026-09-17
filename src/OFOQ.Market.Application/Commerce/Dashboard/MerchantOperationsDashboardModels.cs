namespace OFOQ.Market.Application.Commerce.Dashboard;

public sealed record MerchantDashboardReadinessResult(
    int Percentage,
    string State,
    string StoreStatus);

public sealed record MerchantDashboardRecentOrderResult(
    Guid OrderId,
    string Status,
    string FulfillmentStatus,
    string Currency,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc);

public sealed record MerchantDashboardLowStockResult(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string VariantName,
    string Sku,
    int Quantity,
    int LowStockThreshold);

public sealed record MerchantDashboardAbandonedCartResult(
    Guid CartId,
    Guid? CustomerUserId,
    string? Currency,
    int TotalQuantity,
    decimal TotalAmount,
    DateTimeOffset LastActivityAtUtc);

public sealed record MerchantDashboardTopProductResult(
    Guid ProductId,
    string ProductName,
    string Currency,
    int QuantitySold,
    decimal CapturedSales);

public sealed record MerchantOperationsDashboardResult(
    MerchantDashboardReadinessResult Readiness,
    int OpenOrders,
    int PendingOrders,
    int PaidOrdersAwaitingConfirmation,
    int LowStockVariants,
    int AbandonedCarts,
    int AbandonedAfterMinutes,
    IReadOnlyList<MerchantDashboardRecentOrderResult> RecentOrders,
    IReadOnlyList<MerchantDashboardLowStockResult> LowStock,
    IReadOnlyList<MerchantDashboardAbandonedCartResult> AbandonedCartItems,
    IReadOnlyList<MerchantDashboardTopProductResult> TopProducts);
