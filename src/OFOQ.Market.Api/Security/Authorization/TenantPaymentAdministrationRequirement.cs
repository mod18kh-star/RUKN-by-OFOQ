using Microsoft.AspNetCore.Authorization;

namespace OFOQ.Market.Api.Security.Authorization;

public sealed class TenantPaymentAdministrationRequirement :
    IAuthorizationRequirement
{
    public static TenantPaymentAdministrationRequirement Instance { get; } =
        new();

    private TenantPaymentAdministrationRequirement()
    {
    }
}