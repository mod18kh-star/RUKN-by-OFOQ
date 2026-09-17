using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantStoreSocialLinkRepository
{
    Task<IReadOnlyList<TenantStoreSocialLink>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyCollection<TenantStoreSocialLink> socialLinks,
        CancellationToken cancellationToken = default);

    void RemoveRange(
        IReadOnlyCollection<TenantStoreSocialLink> socialLinks);
}
