namespace OFOQ.Market.Contracts.Commerce.Analytics;

public sealed record MerchantAnalyticsCurrencyResponse(
    string Currency,
    int PaidOrders,
    decimal CapturedSales,
    decimal AverageOrderValue);

public sealed record MerchantAnalyticsTopProductResponse(
    Guid ProductId,
    string ProductName,
    string Currency,
    int QuantitySold,
    decimal CapturedSales);

public sealed record MerchantAnalyticsResponse(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int TotalOrders,
    int CancelledOrders,
    IReadOnlyList<MerchantAnalyticsCurrencyResponse> Sales,
    IReadOnlyList<MerchantAnalyticsTopProductResponse> TopProducts);