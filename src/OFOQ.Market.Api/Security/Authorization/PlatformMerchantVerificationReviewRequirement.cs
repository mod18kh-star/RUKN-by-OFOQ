using Microsoft.AspNetCore.Authorization;

namespace OFOQ.Market.Api.Security.Authorization;

public sealed class PlatformMerchantVerificationReviewRequirement :
    IAuthorizationRequirement
{
    public static PlatformMerchantVerificationReviewRequirement Instance { get; } =
        new();

    private PlatformMerchantVerificationReviewRequirement()
    {
    }
}
