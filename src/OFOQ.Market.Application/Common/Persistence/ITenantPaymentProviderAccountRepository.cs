using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantPaymentProviderAccountRepository
{
    Task<TenantPaymentProviderAccount?> GetByIdAsync(
        TenantPaymentProviderAccountId accountId,
        CancellationToken cancellationToken = default);

    Task<TenantPaymentProviderAccount?> GetByProviderAndEnvironmentAsync(
        PaymentProviderCode providerCode,
        PaymentProviderEnvironment environment,
        CancellationToken cancellationToken = default);

    Task<TenantPaymentProviderAccount?> GetEnabledByProviderAsync(
        PaymentProviderCode providerCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantPaymentProviderAccount>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TenantPaymentProviderAccount account,
        CancellationToken cancellationToken = default);
}