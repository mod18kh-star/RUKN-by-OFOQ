using OFOQ.Market.Domain.Commerce.Verification;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IMerchantVerificationProfileRepository
{
    Task<MerchantVerificationProfile?> GetAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MerchantVerificationProfile profile,
        CancellationToken cancellationToken = default);
}
