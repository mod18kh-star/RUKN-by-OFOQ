using Microsoft.AspNetCore.Authorization;

namespace OFOQ.Market.Api.Security.Authorization;

public sealed class TenantBackOfficeRequirement :
    IAuthorizationRequirement
{
    public static TenantBackOfficeRequirement Instance { get; } =
        new();

    private TenantBackOfficeRequirement()
    {
    }
}