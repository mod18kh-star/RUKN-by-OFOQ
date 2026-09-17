namespace OFOQ.Market.Contracts.Commerce.Dashboard;

public sealed record MerchantDashboardReadinessResponse(
    int Percentage,
    string State,
    string StoreStatus);

public sealed record MerchantDashboardRecentOrderResponse(
    Guid OrderId,
    string Status,
    string FulfillmentStatus,
    string Currency,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc);

public sealed record MerchantDashboardLowStockResponse(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string VariantName,
    string Sku,
    int Quantity,
    int LowStockThreshold);

public sealed record MerchantDashboardAbandonedCartResponse(
    Guid CartId,
    Guid? CustomerUserId,
    string? Currency,
    int TotalQuantity,
    decimal TotalAmount,
    DateTimeOffset LastActivityAtUtc);

public sealed record MerchantDashboardTopProductResponse(
    Guid ProductId,
    string ProductName,
    string Currency,
    int QuantitySold,
    decimal CapturedSales);

public sealed record MerchantOperationsDashboardResponse(
    MerchantDashboardReadinessResponse Readiness,
    int OpenOrders,
    int PendingOrders,
    int PaidOrdersAwaitingConfirmation,
    int LowStockVariants,
    int AbandonedCarts,
    int AbandonedAfterMinutes,
    IReadOnlyList<MerchantDashboardRecentOrderResponse> RecentOrders,
    IReadOnlyList<MerchantDashboardLowStockResponse> LowStock,
    IReadOnlyList<MerchantDashboardAbandonedCartResponse> AbandonedCartItems,
    IReadOnlyList<MerchantDashboardTopProductResponse> TopProducts);
