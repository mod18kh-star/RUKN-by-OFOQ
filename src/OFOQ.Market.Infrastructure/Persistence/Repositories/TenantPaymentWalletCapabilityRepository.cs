using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class TenantPaymentWalletCapabilityRepository :
    ITenantPaymentWalletCapabilityRepository
{
    private readonly MarketDbContext _dbContext;

    public TenantPaymentWalletCapabilityRepository(
        MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TenantPaymentWalletCapability?> GetByIdAsync(
        TenantPaymentWalletCapabilityId capabilityId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Set<TenantPaymentWalletCapability>()
            .SingleOrDefaultAsync(
                capability =>
                    capability.Id == capabilityId,
                cancellationToken);
    }

    public Task<TenantPaymentWalletCapability?> GetByAccountAndWalletAsync(
        TenantPaymentProviderAccountId providerAccountId,
        PaymentWalletType walletType,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Set<TenantPaymentWalletCapability>()
            .SingleOrDefaultAsync(
                capability =>
                    capability.ProviderAccountId == providerAccountId &&
                    capability.WalletType == walletType,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TenantPaymentWalletCapability>> GetByAccountAsync(
        TenantPaymentProviderAccountId providerAccountId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<TenantPaymentWalletCapability>()
            .Where(
                capability =>
                    capability.ProviderAccountId == providerAccountId)
            .OrderBy(
                capability =>
                    capability.WalletType)
            .ToListAsync(
                cancellationToken);
    }

    public Task AddAsync(
        TenantPaymentWalletCapability capability,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            capability);

        _dbContext
            .Set<TenantPaymentWalletCapability>()
            .Add(capability);

        return Task.CompletedTask;
    }
}