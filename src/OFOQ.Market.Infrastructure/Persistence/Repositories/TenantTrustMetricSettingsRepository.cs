using Microsoft.EntityFrameworkCore; using OFOQ.Market.Application.Common.Persistence; using OFOQ.Market.Domain.Commerce.Reviews;
namespace OFOQ.Market.Infrastructure.Persistence.Repositories;
internal sealed class TenantTrustMetricSettingsRepository:ITenantTrustMetricSettingsRepository
{
 private readonly MarketDbContext _db; public TenantTrustMetricSettingsRepository(MarketDbContext db)=>_db=db;
 public Task<TenantTrustMetricSettings?> GetAsync(CancellationToken cancellationToken=default)=>_db.TenantTrustMetricSettings.SingleOrDefaultAsync(cancellationToken);
 public Task AddAsync(TenantTrustMetricSettings settings,CancellationToken cancellationToken=default)=>_db.TenantTrustMetricSettings.AddAsync(settings,cancellationToken).AsTask();
}
