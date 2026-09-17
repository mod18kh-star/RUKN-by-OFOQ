using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Application.Tenancy.StoreProfile;
using OFOQ.Market.Application.Tenancy.StoreReadiness;
using OFOQ.Market.Contracts.Tenancy;

namespace OFOQ.Market.Api.Endpoints.Tenancy;

public static class TenantStoreProfileEndpoints
{
    public static IEndpointRouteBuilder MapTenantStoreProfileEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/store-profile")
                .WithTags(
                    "Tenant Store Profile")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        group.MapGet(
            "",
            GetProfileAsync);

        group.MapPut(
            "",
            UpdateProfileAsync);

        group.MapPut(
            "/social-links",
            ReplaceSocialLinksAsync);

        group.MapGet(
            "/readiness",
            GetReadinessAsync);

        return endpoints;
    }

    private static async Task<IResult> GetProfileAsync(
        GetStoreProfileHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await handler.HandleAsync(
                    cancellationToken);

            return result is null
                ? Results.NotFound(
                    new
                    {
                        code =
                            "tenant_not_found",

                        message =
                            "Tenant was not found."
                    })
                : Results.Ok(
                    MapProfile(
                        result));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> UpdateProfileAsync(
        UpdateStoreProfileRequest request,
        UpdateStoreProfileHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actorUserId =
            GetActorUserId(
                httpContext);

        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        try
        {
            await handler.HandleAsync(
                new UpdateStoreProfileCommand(
                    request.WebsiteUrl,
                    request.WhatsAppNumber,
                    request.CustomerServicePhone,
                    request.CommercialRegistrationNumber,
                    request.CommercialRegistrationNotApplicable,
                    actorUserId.Value),
                cancellationToken);

            return Results.NoContent();
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                exception.Message);
        }
    }

    private static async Task<IResult> ReplaceSocialLinksAsync(
        ReplaceStoreSocialLinksRequest request,
        ReplaceStoreSocialLinksHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actorUserId =
            GetActorUserId(
                httpContext);

        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        try
        {
            await handler.HandleAsync(
                new ReplaceStoreSocialLinksCommand(
                    request.SocialLinks
                        .Select(
                            item =>
                                new ReplaceStoreSocialLinkInput(
                                    item.PlatformCode,
                                    item.Label,
                                    item.Url,
                                    item.IsVisible))
                        .ToArray(),
                    actorUserId.Value),
                cancellationToken);

            return Results.NoContent();
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                exception.Message);
        }
    }

    private static async Task<IResult> GetReadinessAsync(
        GetStoreReadinessHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await handler.HandleAsync(
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

            return Results.Ok(
                new StoreReadinessResponse(
                    result.Percentage,
                    result.State,
                    result.StoreStatus,
                    result.Items
                        .Select(
                            item =>
                                new StoreReadinessItemResponse(
                                    item.Code,
                                    item.Weight,
                                    item.Completed,
                                    item.MerchantActionRequired))
                        .ToArray()));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static StoreProfileResponse MapProfile(
        StoreProfileResult result)
    {
        return new StoreProfileResponse(
            result.TenantId,
            result.Name,
            result.Slug,
            result.Status,
            result.WebsiteUrl,
            result.WhatsAppNumber,
            result.CustomerServicePhone,
            result.CommercialRegistrationNumber,
            result.CommercialRegistrationNotApplicable,
            result.SocialLinks
                .Select(
                    item =>
                        new StoreSocialLinkResponse(
                            item.SocialLinkId,
                            item.PlatformCode,
                            item.Label,
                            item.Url,
                            item.SortOrder,
                            item.IsVisible))
                .ToArray());
    }

    private static Guid? GetActorUserId(
        HttpContext httpContext)
    {
        var subject =
            httpContext.User
                .FindFirst(
                    JwtRegisteredClaimNames.Sub)?
                .Value;

        if (!Guid.TryParse(
                subject,
                out var userId) ||
            userId ==
                Guid.Empty)
        {
            return null;
        }

        return userId;
    }

    private static IResult ValidationError(
        string message)
    {
        return Results.BadRequest(
            new
            {
                code =
                    "store_profile_invalid",

                message
            });
    }
}
