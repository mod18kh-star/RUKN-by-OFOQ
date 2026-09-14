namespace OFOQ.Market.Application.Commerce.Analytics;

public sealed record GetMerchantAnalyticsQuery(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    int TopProducts = 5);