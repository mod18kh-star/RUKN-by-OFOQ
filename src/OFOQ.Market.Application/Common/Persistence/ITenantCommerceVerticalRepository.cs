using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantCommerceVerticalRepository
{
    Task<IReadOnlyList<TenantCommerceVertical>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<TenantCommerceVertical?> GetByTypeAsync(
        CommerceVerticalType verticalType,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TenantCommerceVertical vertical,
        CancellationToken cancellationToken = default);
}