using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Persistence;

public sealed record StoreReadinessData(
    bool HasPrimaryVertical,
    int ProductCount,
    int PublishedProductCount,
    int VisibleSocialLinkCount);

public interface IStoreReadinessQueryRepository
{
    Task<StoreReadinessData> GetAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);
}
