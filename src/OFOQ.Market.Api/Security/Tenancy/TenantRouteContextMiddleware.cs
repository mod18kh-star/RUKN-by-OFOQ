using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Security.Tenancy;

public sealed class TenantRouteContextMiddleware
{
    private const string TenantRouteKey =
        "tenantId";

    private readonly RequestDelegate
        _next;

    public TenantRouteContextMiddleware(
        RequestDelegate next)
    {
        _next =
            next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        CurrentTenantContext currentTenantContext)
    {
        if (httpContext.Request.RouteValues
                .TryGetValue(
                    TenantRouteKey,
                    out var routeValue) &&
            Guid.TryParse(
                routeValue?.ToString(),
                out var tenantGuid) &&
            tenantGuid != Guid.Empty)
        {
            currentTenantContext.SetTenant(
                TenantId.From(
                    tenantGuid));
        }

        await _next(
            httpContext);
    }
}