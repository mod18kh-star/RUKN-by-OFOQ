using OFOQ.Market.Domain.Commerce.Fulfillment;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IShippingMethodRepository
{
    Task<IReadOnlyList<ShippingMethod>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ShippingMethod?> GetByIdAsync(ShippingMethodId id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string normalizedCode, ShippingMethodId? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(ShippingMethod method, CancellationToken cancellationToken = default);
}
