using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Application.Commerce.Configuration.Common;
using OFOQ.Market.Application.Commerce.Configuration.ConfigureVertical;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Configuration;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class CommerceOnboardingEndpoints
{
    public static IEndpointRouteBuilder MapCommerceOnboardingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGroup(
                "/api/tenants/{tenantId:guid}/onboarding/commerce")
            .WithTags(
                "Commerce Onboarding")
            .RequireAuthorization()
            .MapPut(
                "/vertical",
                ConfigureInitialVerticalAsync);

        return endpoints;
    }

    private static async Task<IResult> ConfigureInitialVerticalAsync(
        Guid tenantId,
        ConfigureCommerceVerticalRequest request,
        ConfigureCommerceVerticalHandler configureVerticalHandler,
        ITenantRepository tenantRepository,
        ITenantMembershipRepository membershipRepository,
        ITenantCommerceVerticalRepository verticalRepository,
        ICurrentTenant currentTenant,
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

        if (tenantId ==
            Guid.Empty)
        {
            return ValidationError(
                "tenant_id_invalid",
                "Tenant ID must be a valid non-empty GUID.");
        }

        if (!TryParseVerticalType(
                request.VerticalType,
                out var verticalType))
        {
            return ValidationError(
                "commerce_vertical_invalid",
                "A supported commerce vertical type is required.");
        }

        /*
         * This endpoint is intentionally much narrower than
         * TenantCommerceAdministration.
         *
         * It exists only so the creator/owner can configure the
         * first commerce vertical while the tenant is still Draft.
         *
         * It does NOT allow:
         * - capability administration
         * - changing an existing primary vertical
         * - configuring an Active tenant
         * - non-owner access
         */
        var routeTenantId =
            TenantId.From(
                tenantId);

        if (!currentTenant.IsAvailable ||
            !currentTenant.TenantId.HasValue ||
            currentTenant.TenantId.Value !=
                routeTenantId)
        {
            return Results.Forbid();
        }

        try
        {
            var tenant =
                await tenantRepository
                    .GetByIdAsync(
                        routeTenantId,
                        cancellationToken);

            if (tenant is null ||
                tenant.IsDeleted)
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

            if (tenant.Status !=
                TenantStatus.Draft)
            {
                return Conflict(
                    "commerce_onboarding_tenant_not_draft",
                    "Initial commerce setup is only available while the tenant is in Draft status.");
            }

            var membership =
                await membershipRepository
                    .GetByTenantAndUserAsync(
                        routeTenantId,
                        actorUserId.Value,
                        cancellationToken);

            if (membership is null ||
                membership.IsDeleted ||
                membership.Role !=
                    TenantRole.Owner)
            {
                return Results.Forbid();
            }

            var configuredVerticals =
                await verticalRepository
                    .GetAllAsync(
                        cancellationToken);

            /*
             * Make retries idempotent:
             *
             * If the exact same initial vertical was already written
             * successfully but the frontend lost the response, return
             * success instead of failing or creating anything twice.
             */
            if (configuredVerticals.Count >
                0)
            {
                var sameInitialVertical =
                    configuredVerticals
                        .SingleOrDefault(
                            vertical =>
                                vertical.VerticalType ==
                                    verticalType &&
                                vertical.IsEnabled &&
                                vertical.IsPrimary);

                if (sameInitialVertical is not null &&
                    configuredVerticals.Count ==
                        1)
                {
                    return Results.Ok(
                        MapVertical(
                            sameInitialVertical));
                }

                return Conflict(
                    "commerce_onboarding_vertical_already_configured",
                    "The initial commerce vertical has already been configured. Further changes require the protected commerce administration flow.");
            }

            /*
             * Onboarding always creates the first vertical enabled
             * and primary. Request flags cannot be used to weaken
             * that invariant.
             */
            var result =
                await configureVerticalHandler
                    .HandleAsync(
                        new ConfigureCommerceVerticalCommand(
                            verticalType,
                            IsEnabled: true,
                            IsPrimary: true,
                            actorUserId.Value.Value),
                        cancellationToken);

            return Results.Ok(
                new CommerceVerticalResponse(
                    result.VerticalType.ToString(),
                    result.Code,
                    result.IsEnabled,
                    result.IsPrimary));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (CommerceConfigurationConflictException exception)
        {
            return Conflict(
                "commerce_configuration_conflict",
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                "commerce_configuration_invalid",
                exception.Message);
        }
    }

    private static CommerceVerticalResponse MapVertical(
        TenantCommerceVertical vertical)
    {
        var definition =
            CommerceVerticalCatalog.Get(
                vertical.VerticalType);

        return new CommerceVerticalResponse(
            vertical.VerticalType.ToString(),
            definition.Code,
            vertical.IsEnabled,
            vertical.IsPrimary);
    }

    private static UserId? GetActorUserId(
        HttpContext httpContext)
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
            return null;
        }

        return UserId.From(
            userGuid);
    }

    private static bool TryParseVerticalType(
        string? value,
        out CommerceVerticalType verticalType)
    {
        verticalType =
            CommerceVerticalType.Unknown;

        if (string.IsNullOrWhiteSpace(
                value))
        {
            return false;
        }

        if (!Enum.TryParse(
                value.Trim(),
                ignoreCase: true,
                out verticalType))
        {
            return false;
        }

        if (verticalType ==
            CommerceVerticalType.Unknown ||
            !Enum.IsDefined(
                verticalType))
        {
            return false;
        }

        return CommerceVerticalCatalog.TryGet(
            verticalType,
            out _);
    }

    private static IResult ValidationError(
        string code,
        string message)
    {
        return Results.BadRequest(
            new
            {
                code,
                message
            });
    }

    private static IResult Conflict(
        string code,
        string message)
    {
        return Results.Conflict(
            new
            {
                code,
                message
            });
    }
}