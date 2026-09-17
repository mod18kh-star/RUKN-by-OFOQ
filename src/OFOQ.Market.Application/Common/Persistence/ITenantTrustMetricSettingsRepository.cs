using OFOQ.Market.Domain.Commerce.Reviews;
namespace OFOQ.Market.Application.Common.Persistence;
public interface ITenantTrustMetricSettingsRepository
{
 Task<TenantTrustMetricSettings?> GetAsync(CancellationToken cancellationToken=default);
 Task AddAsync(TenantTrustMetricSettings settings,CancellationToken cancellationToken=default);
}
