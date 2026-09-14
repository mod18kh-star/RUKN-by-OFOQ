using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IMerchantVerificationReviewRepository
{
    Task<MerchantVerificationProfile?>
        GetProfileByIdAsync(
            MerchantVerificationProfileId profileId,
            CancellationToken cancellationToken = default);

    Task<bool>
        HasActiveTenantMembershipAsync(
            TenantId tenantId,
            UserId userId,
            CancellationToken cancellationToken = default);

    Task<int>
        SaveChangesAsync(
            CancellationToken cancellationToken = default);
}
