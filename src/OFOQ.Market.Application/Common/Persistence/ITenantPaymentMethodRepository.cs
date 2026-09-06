using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantPaymentMethodRepository
{
    Task<TenantPaymentMethod?> GetByIdAsync(
        TenantPaymentMethodId paymentMethodId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantPaymentMethod>> GetEnabledAsync(
        CurrencyCode currency,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TenantPaymentMethod paymentMethod,
        CancellationToken cancellationToken = default);
}
