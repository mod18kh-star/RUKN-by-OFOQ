using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Application.Notifications;
using OFOQ.Market.Contracts.Notifications;

namespace OFOQ.Market.Api.Endpoints.Tenancy;

public static class NotificationPreferenceEndpoints
{
    public static IEndpointRouteBuilder MapNotificationPreferenceEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/notification-preferences")
                .WithTags(
                    "Back Office Notification Preferences")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        group.MapGet(
            "/",
            GetAsync);

        group.MapPut(
            "/",
            UpdateAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        GetNotificationPreferencesHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await handler.HandleAsync(
                    cancellationToken);

            return Results.Ok(
                Map(result));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> UpdateAsync(
        UpdateNotificationPreferencesRequest request,
        ClaimsPrincipal principal,
        UpdateNotificationPreferencesHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            Guid? actorUserId =
                null;

            var subject =
                principal.FindFirst(
                    JwtRegisteredClaimNames.Sub)?
                    .Value;

            if (Guid.TryParse(
                    subject,
                    out var parsed) &&
                parsed != Guid.Empty)
            {
                actorUserId = parsed;
            }

            var result =
                await handler.HandleAsync(
                    new UpdateNotificationPreferencesCommand(
                        request.NewOrderEmailEnabled,
                        request.LowStockEmailEnabled,
                        request.ReviewEmailEnabled,
                        request.PlatformRequestEmailEnabled,
                        actorUserId),
                    cancellationToken);

            return Results.Ok(
                Map(result));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static NotificationPreferencesResponse Map(
        NotificationPreferencesResult result)
        => new(
            result.NewOrderEmailEnabled,
            result.LowStockEmailEnabled,
            result.ReviewEmailEnabled,
            result.PlatformRequestEmailEnabled);
}
