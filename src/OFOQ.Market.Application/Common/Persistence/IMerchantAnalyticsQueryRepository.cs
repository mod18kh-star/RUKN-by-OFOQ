using OFOQ.Market.Application.Commerce.Analytics;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IMerchantAnalyticsQueryRepository
{
    Task<MerchantAnalyticsResult> GetAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int topProducts,
        CancellationToken cancellationToken = default);
}