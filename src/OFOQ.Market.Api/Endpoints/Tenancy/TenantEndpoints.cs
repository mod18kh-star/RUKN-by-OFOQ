using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Tenancy.CreateTenant;
using OFOQ.Market.Application.Tenancy.GetTenantById;
using OFOQ.Market.Contracts.Tenancy;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Endpoints.Tenancy;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants")
                .WithTags(
                    "Tenants");

        /*
         * Creating a store requires authentication.
         *
         * MFA is NOT required here because a new merchant may
         * create their first store before enrolling in MFA.
         *
         * Operational Back Office access remains protected by
         * the stricter tenant-backoffice MFA policy.
         */
        group.MapPost(
                "/",
                CreateTenantAsync)
            .RequireAuthorization();

        /*
         * Slug availability is part of the authenticated onboarding
         * flow. It checks every tenant state, not just active stores,
         * so a merchant never sees a false "available" result for a
         * slug already reserved by a draft/suspended tenant.
         */
        group.MapGet(
                "/slug-availability/{slug}",
                GetSlugAvailabilityAsync)
            .RequireAuthorization();

        /*
         * Public tenant metadata may be required by storefront
         * discovery, so this route remains public for now.
         */
        group.MapGet(
            "/{tenantId}",
            GetTenantByIdAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateTenantAsync(
        CreateTenantRequest request,
        CreateTenantHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var subject =
            httpContext.User
                .FindFirst(
                    JwtRegisteredClaimNames.Sub)?
                .Value;

        if (!Guid.TryParse(
                subject,
                out var creatorUserGuid) ||
            creatorUserGuid == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        try
        {
            var command =
                new CreateTenantCommand(
                    request.Name,
                    request.Slug,
                    UserId.From(
                        creatorUserGuid));

            var result =
                await handler.HandleAsync(
                    command,
                    cancellationToken);

            var response =
                new CreateTenantResponse(
                    result.TenantId.Value,
                    result.Name,
                    result.Slug,
                    result.Status.ToString());

            return Results.Created(
                $"/api/tenants/{result.TenantId.Value}",
                response);
        }
        catch (TenantCreationNotAllowedException)
        {
            return Results.Forbid();
        }
        catch (TenantSlugAlreadyExistsException exception)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "tenant_slug_already_exists",

                    message =
                        exception.Message,

                    slug =
                        exception.Slug.Value
                });
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "validation_error",

                    message =
                        exception.Message
                });
        }
    }

    private static async Task<IResult> GetSlugAvailabilityAsync(
        string slug,
        ITenantRepository tenantRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            var tenantSlug =
                TenantSlug.Create(
                    slug);

            var exists =
                await tenantRepository
                    .SlugExistsAsync(
                        tenantSlug,
                        excludingTenantId: null,
                        cancellationToken);

            return Results.Ok(
                new TenantSlugAvailabilityResponse(
                    tenantSlug.Value,
                    !exists));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "invalid_tenant_slug",

                    message =
                        exception.Message
                });
        }
    }

    private static async Task<IResult> GetTenantByIdAsync(
        string tenantId,
        GetTenantByIdHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(
                tenantId,
                out var tenantGuid) ||
            tenantGuid == Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "invalid_tenant_id",

                    message =
                        "Tenant ID must be a valid non-empty GUID."
                });
        }

        var result =
            await handler.HandleAsync(
                TenantId.From(
                    tenantGuid),
                cancellationToken);

        if (result is null)
        {
            return Results.NotFound(
                new
                {
                    code =
                        "tenant_not_found",

                    message =
                        "Tenant was not found."
                });
        }

        var response =
            new GetTenantByIdResponse(
                result.TenantId.Value,
                result.Name,
                result.Slug,
                result.Status.ToString(),
                result.CreatedAtUtc);

        return Results.Ok(
            response);
    }
}
