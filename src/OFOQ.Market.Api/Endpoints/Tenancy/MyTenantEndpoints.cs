using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Tenancy;

public static class MyTenantEndpoints
{
    public static IEndpointRouteBuilder MapMyTenantEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(
                "/api/tenants/mine",
                GetMyTenantsAsync)
            .WithTags(
                "Tenancy")
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> GetMyTenantsAsync(
        HttpContext httpContext,
        ITenantMembershipRepository membershipRepository,
        ITenantRepository tenantRepository,
        CancellationToken cancellationToken)
    {
        var subject =
            httpContext.User
                .FindFirst(
                    JwtRegisteredClaimNames.Sub)?
                .Value;

        if (!Guid.TryParse(
                subject,
                out var userGuid) ||
            userGuid ==
                Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var userId =
            UserId.From(
                userGuid);

        var memberships =
            await membershipRepository
                .GetByUserIdAsync(
                    userId,
                    cancellationToken);

        var result =
            new List<MyTenantResponse>();

        foreach (
            var membership in
            memberships
                .Where(
                    item =>
                        !item.IsDeleted)
                .OrderByDescending(
                    item =>
                        item.CreatedAtUtc))
        {
            var tenant =
                await tenantRepository
                    .GetByIdAsync(
                        membership.TenantId,
                        cancellationToken);

            if (tenant is null ||
                tenant.IsDeleted)
            {
                continue;
            }

            result.Add(
                new MyTenantResponse(
                    tenant.Id.Value,
                    tenant.Name,
                    tenant.Slug.ToString(),
                    tenant.Status.ToString(),
                    membership.Role.ToString(),
                    membership.CreatedAtUtc));
        }

        return Results.Ok(
            result);
    }

    private sealed record MyTenantResponse(
        Guid TenantId,
        string Name,
        string Slug,
        string Status,
        string Role,
        DateTimeOffset MembershipCreatedAtUtc);
}