using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class TenantPaymentProviderAccountRepository :
    ITenantPaymentProviderAccountRepository
{
    private readonly MarketDbContext _dbContext;

    public TenantPaymentProviderAccountRepository(
        MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TenantPaymentProviderAccount?> GetByIdAsync(
        TenantPaymentProviderAccountId accountId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Set<TenantPaymentProviderAccount>()
            .SingleOrDefaultAsync(
                account =>
                    account.Id == accountId,
                cancellationToken);
    }

    public Task<TenantPaymentProviderAccount?> GetByProviderAndEnvironmentAsync(
        PaymentProviderCode providerCode,
        PaymentProviderEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        if (providerCode.IsEmpty)
        {
            throw new ArgumentException(
                "Payment provider code is required.",
                nameof(providerCode));
        }

        return _dbContext
            .Set<TenantPaymentProviderAccount>()
            .SingleOrDefaultAsync(
                account =>
                    EF.Property<string>(
                        account,
                        "_providerCode") == providerCode.Value &&
                    account.Environment == environment,
                cancellationToken);
    }

    public Task<TenantPaymentProviderAccount?> GetEnabledByProviderAsync(
        PaymentProviderCode providerCode,
        CancellationToken cancellationToken = default)
    {
        if (providerCode.IsEmpty)
        {
            throw new ArgumentException(
                "Payment provider code is required.",
                nameof(providerCode));
        }

        return _dbContext
            .Set<TenantPaymentProviderAccount>()
            .SingleOrDefaultAsync(
                account =>
                    EF.Property<string>(
                        account,
                        "_providerCode") == providerCode.Value &&
                    account.IsEnabled,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TenantPaymentProviderAccount>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<TenantPaymentProviderAccount>()
            .OrderBy(account => account.CreatedAtUtc)
            .ToListAsync(
                cancellationToken);
    }

    public Task AddAsync(
        TenantPaymentProviderAccount account,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            account);

        _dbContext
            .Set<TenantPaymentProviderAccount>()
            .Add(account);

        return Task.CompletedTask;
    }
}