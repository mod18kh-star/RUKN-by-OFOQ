namespace OFOQ.Market.Application.Commerce.Analytics;

public sealed record MerchantAnalyticsCurrencyResult(
    string Currency,
    int PaidOrders,
    decimal CapturedSales,
    decimal AverageOrderValue);

public sealed record MerchantAnalyticsTopProductResult(
    Guid ProductId,
    string ProductName,
    string Currency,
    int QuantitySold,
    decimal CapturedSales);

public sealed record MerchantAnalyticsResult(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int TotalOrders,
    int CancelledOrders,
    IReadOnlyList<MerchantAnalyticsCurrencyResult> Sales,
    IReadOnlyList<MerchantAnalyticsTopProductResult> TopProducts);