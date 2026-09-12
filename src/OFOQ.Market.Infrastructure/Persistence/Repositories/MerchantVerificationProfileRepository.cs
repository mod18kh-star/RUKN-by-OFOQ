using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Verification;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class MerchantVerificationProfileRepository :
    IMerchantVerificationProfileRepository
{
    private readonly MarketDbContext
        _dbContext;

    public MerchantVerificationProfileRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<MerchantVerificationProfile?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .Set<MerchantVerificationProfile>()
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        MerchantVerificationProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        await _dbContext
            .Set<MerchantVerificationProfile>()
            .AddAsync(
                profile,
                cancellationToken);
    }
}
