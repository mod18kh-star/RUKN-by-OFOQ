using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class TenantPaymentCapabilityRepository :
    ITenantPaymentCapabilityRepository
{
    private readonly MarketDbContext _dbContext;

    public TenantPaymentCapabilityRepository(MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TenantPaymentCapability?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantPaymentCapabilities.SingleOrDefaultAsync(
            cancellationToken);
    }

    public Task AddAsync(
        TenantPaymentCapability capability,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capability);

        return _dbContext.TenantPaymentCapabilities
            .AddAsync(capability, cancellationToken)
            .AsTask();
    }
}
