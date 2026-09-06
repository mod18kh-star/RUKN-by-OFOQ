using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class TenantPaymentMethodRepository :
    ITenantPaymentMethodRepository
{
    private readonly MarketDbContext _dbContext;

    public TenantPaymentMethodRepository(MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TenantPaymentMethod?> GetByIdAsync(
        TenantPaymentMethodId paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantPaymentMethods.SingleOrDefaultAsync(
            method => method.Id == paymentMethodId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<TenantPaymentMethod>> GetEnabledAsync(
        CurrencyCode currency,
        CancellationToken cancellationToken = default)
    {
        if (currency.IsEmpty)
        {
            throw new ArgumentException(
                "Currency is required.",
                nameof(currency));
        }

        return await _dbContext.TenantPaymentMethods
            .Where(method =>
                method.IsEnabled &&
                EF.Property<string>(method, "_currencyCode") == currency.Value)
            .OrderBy(method => method.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(
        TenantPaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentMethod);

        return _dbContext.TenantPaymentMethods
            .AddAsync(paymentMethod, cancellationToken)
            .AsTask();
    }
}
