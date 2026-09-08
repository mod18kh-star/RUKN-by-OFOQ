using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Commerce.Configuration.CapabilityOverrides;
using OFOQ.Market.Application.Commerce.Configuration.Common;
using OFOQ.Market.Application.Commerce.Configuration.ConfigureVertical;
using OFOQ.Market.Application.Commerce.Configuration.GetProfile;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Configuration;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class CommerceConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapCommerceConfigurationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/commerce")
                .WithTags(
                    "Commerce Configuration")
                .RequireAuthorization(
                    AuthorizationPolicies
                        .TenantCommerceAdministration);

        group.MapGet(
            "/profile",
            GetProfileAsync);

        group.MapPut(
            "/verticals",
            ConfigureVerticalAsync);

        group.MapPut(
            "/capabilities",
            SetCapabilityOverrideAsync);

        return endpoints;
    }

    private static async Task<IResult> GetProfileAsync(
        GetCommerceProfileHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await handler.HandleAsync(
                    new GetCommerceProfileQuery(),
                    cancellationToken);

            return Results.Ok(
                MapProfile(
                    result));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> ConfigureVerticalAsync(
        ConfigureCommerceVerticalRequest request,
        ConfigureCommerceVerticalHandler handler,
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

        if (!TryParseVerticalType(
                request.VerticalType,
                out var verticalType))
        {
            return ValidationError(
                "commerce_vertical_invalid",
                "A supported commerce vertical type is required.");
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new ConfigureCommerceVerticalCommand(
                        verticalType,
                        request.Enabled,
                        request.Primary,
                        actorUserId.Value.Value),
                    cancellationToken);

            return Results.Ok(
                MapVertical(
                    result));
        }
        catch (CommerceConfigurationConflictException exception)
        {
            return Conflict(
                "commerce_configuration_conflict",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                "commerce_configuration_invalid",
                exception.Message);
        }
    }

    private static async Task<IResult> SetCapabilityOverrideAsync(
        SetCommerceCapabilityOverrideRequest request,
        SetCommerceCapabilityOverrideHandler handler,
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

        if (!TryParseCapabilityType(
                request.CapabilityType,
                out var capabilityType))
        {
            return ValidationError(
                "commerce_capability_invalid",
                "A supported commerce capability type is required.");
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new SetCommerceCapabilityOverrideCommand(
                        capabilityType,
                        request.Enabled,
                        actorUserId.Value.Value),
                    cancellationToken);

            return Results.Ok(
                MapProfile(
                    result));
        }
        catch (CommerceConfigurationConflictException exception)
        {
            return Conflict(
                "commerce_configuration_conflict",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                "commerce_configuration_invalid",
                exception.Message);
        }
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
            userGuid == Guid.Empty)
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
            CommerceVerticalType.Unknown)
        {
            return false;
        }

        if (!Enum.IsDefined(
                verticalType))
        {
            return false;
        }

        return CommerceVerticalCatalog.TryGet(
            verticalType,
            out _);
    }

    private static bool TryParseCapabilityType(
        string? value,
        out CommerceCapabilityType capabilityType)
    {
        capabilityType =
            CommerceCapabilityType.Unknown;

        if (string.IsNullOrWhiteSpace(
                value))
        {
            return false;
        }

        if (!Enum.TryParse(
                value.Trim(),
                ignoreCase: true,
                out capabilityType))
        {
            return false;
        }

        return capabilityType !=
               CommerceCapabilityType.Unknown &&
               Enum.IsDefined(
                   capabilityType);
    }

    private static CommerceProfileResponse MapProfile(
        CommerceProfileResult result)
    {
        return new CommerceProfileResponse(
            result.Verticals
                .Select(
                    MapVertical)
                .ToArray(),
            result.Capabilities
                .Select(
                    capability =>
                        new CommerceCapabilityResponse(
                            capability.CapabilityType.ToString(),
                            capability.IsEnabled,
                            capability.IsOverridden))
                .ToArray());
    }

    private static CommerceVerticalResponse MapVertical(
        CommerceVerticalResult result)
    {
        return new CommerceVerticalResponse(
            result.VerticalType.ToString(),
            result.Code,
            result.IsEnabled,
            result.IsPrimary);
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