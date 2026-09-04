using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Api.Endpoints.Tenancy;

public static class TenantBackOfficeEndpoints
{
    public static IEndpointRouteBuilder
        MapTenantBackOfficeEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice")
                .WithTags(
                    "Tenant Back Office")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        group.MapGet(
            "/context",
            (ICurrentTenant currentTenant) =>
            {
                if (!currentTenant.IsAvailable ||
                    !currentTenant.TenantId.HasValue)
                {
                    return Results.Problem(
                        statusCode:
                            StatusCodes.Status500InternalServerError,

                        title:
                            "Tenant context was not resolved.");
                }

                return Results.Ok(
                    new
                    {
                        tenantId =
                            currentTenant
                                .TenantId
                                .Value
                                .Value
                    });
            });

        return endpoints;
    }
}