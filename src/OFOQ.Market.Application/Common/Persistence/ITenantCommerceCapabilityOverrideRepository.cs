using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantCommerceCapabilityOverrideRepository
{
    Task<IReadOnlyList<TenantCommerceCapabilityOverride>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<TenantCommerceCapabilityOverride?> GetByCapabilityAsync(
        CommerceCapabilityType capabilityType,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TenantCommerceCapabilityOverride capabilityOverride,
        CancellationToken cancellationToken = default);

    void Remove(
        TenantCommerceCapabilityOverride capabilityOverride);
}