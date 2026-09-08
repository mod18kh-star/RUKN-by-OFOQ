using Microsoft.AspNetCore.Authorization;

namespace OFOQ.Market.Api.Security.Authorization;

public sealed class TenantCommerceAdministrationRequirement :
    IAuthorizationRequirement
{
    public static TenantCommerceAdministrationRequirement Instance { get; } =
        new();

    private TenantCommerceAdministrationRequirement()
    {
    }
}