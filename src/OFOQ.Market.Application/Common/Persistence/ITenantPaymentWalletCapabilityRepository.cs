using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Common.Persistence;

public interface ITenantPaymentWalletCapabilityRepository
{
    Task<TenantPaymentWalletCapability?> GetByIdAsync(
        TenantPaymentWalletCapabilityId capabilityId,
        CancellationToken cancellationToken = default);

    Task<TenantPaymentWalletCapability?> GetByAccountAndWalletAsync(
        TenantPaymentProviderAccountId providerAccountId,
        PaymentWalletType walletType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantPaymentWalletCapability>> GetByAccountAsync(
        TenantPaymentProviderAccountId providerAccountId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TenantPaymentWalletCapability capability,
        CancellationToken cancellationToken = default);
}