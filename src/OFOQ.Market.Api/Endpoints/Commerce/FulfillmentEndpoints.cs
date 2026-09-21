using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Commerce.Fulfillment;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Fulfillment;
using OFOQ.Market.Domain.Commerce.Fulfillment;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class FulfillmentEndpoints
{
    public static IEndpointRouteBuilder MapFulfillmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var backoffice = endpoints.MapGroup("/api/tenants/{tenantId:guid}/backoffice/fulfillment").WithTags("Fulfillment").RequireAuthorization(AuthorizationPolicies.TenantBackOffice);
        backoffice.MapGet("/locations", GetLocationsAsync);
        backoffice.MapPost("/locations", CreateLocationAsync);
        backoffice.MapPut("/locations/{id:guid}", UpdateLocationAsync);
        backoffice.MapGet("/shipping-methods", GetShippingMethodsAsync);
        backoffice.MapPost("/shipping-methods", CreateShippingMethodAsync);
        backoffice.MapPut("/shipping-methods/{id:guid}", UpdateShippingMethodAsync);

        var publicGroup = endpoints.MapGroup("/api/tenants/{tenantId:guid}/shipping").WithTags("Shipping");
        publicGroup.MapGet("/methods", GetPublicShippingMethodsAsync);
        return endpoints;
    }

    private static async Task<IResult> GetLocationsAsync(FulfillmentSettingsService service, CancellationToken ct) => Results.Ok((await service.GetLocationsAsync(ct)).Select(Map).ToArray());
    private static async Task<IResult> GetShippingMethodsAsync(FulfillmentSettingsService service, CancellationToken ct) => Results.Ok((await service.GetShippingMethodsAsync(false, ct)).Select(Map).ToArray());
    private static async Task<IResult> GetPublicShippingMethodsAsync(FulfillmentSettingsService service, CancellationToken ct) => Results.Ok((await service.GetShippingMethodsAsync(true, ct)).Select(Map).ToArray());

    private static async Task<IResult> CreateLocationAsync(FulfillmentLocationRequest request, FulfillmentSettingsService service, HttpContext http, CancellationToken ct)
    {
        var actor = Actor(http); if (!actor.HasValue) return Results.Unauthorized();
        try { return Results.Ok(Map(await service.CreateLocationAsync(request.Code, request.Name, request.Phone, request.CountryCode, request.City, request.Region, request.Line1, request.Line2, request.IsDefault, actor.Value, ct))); }
        catch (TenantScopeViolationException) { return Results.Forbid(); }
        catch (ArgumentException e) { return Validation(e.Message); }
    }

    private static async Task<IResult> UpdateLocationAsync(Guid id, FulfillmentLocationRequest request, FulfillmentSettingsService service, HttpContext http, CancellationToken ct)
    {
        var actor = Actor(http); if (!actor.HasValue) return Results.Unauthorized();
        try { var x = await service.UpdateLocationAsync(FulfillmentLocationId.From(id), request.Code, request.Name, request.Phone, request.CountryCode, request.City, request.Region, request.Line1, request.Line2, request.IsActive, request.IsDefault, actor.Value, ct); return x is null ? Results.NotFound() : Results.Ok(Map(x)); }
        catch (TenantScopeViolationException) { return Results.Forbid(); }
        catch (ArgumentException e) { return Validation(e.Message); }
    }

    private static async Task<IResult> CreateShippingMethodAsync(ShippingMethodRequest request, FulfillmentSettingsService service, HttpContext http, CancellationToken ct)
    {
        var actor = Actor(http); if (!actor.HasValue) return Results.Unauthorized();
        if (!Enum.TryParse<ShippingMethodType>(request.Type, true, out var type) || !Enum.IsDefined(type)) return Validation("Unsupported shipping method type.");
        try { return Results.Ok(Map(await service.CreateShippingMethodAsync(request.Code, request.Name, type, request.Price, request.Currency, request.MinimumOrderAmount, request.MaximumOrderAmount, request.PickupLocationId, request.SortOrder, actor.Value, ct, isEnabled: request.IsEnabled))); }
        catch (TenantScopeViolationException) { return Results.Forbid(); }
        catch (ArgumentException e) { return Validation(e.Message); }
    }

    private static async Task<IResult> UpdateShippingMethodAsync(Guid id, ShippingMethodRequest request, FulfillmentSettingsService service, HttpContext http, CancellationToken ct)
    {
        var actor = Actor(http); if (!actor.HasValue) return Results.Unauthorized();
        if (!Enum.TryParse<ShippingMethodType>(request.Type, true, out var type) || !Enum.IsDefined(type)) return Validation("Unsupported shipping method type.");
        try { var x = await service.UpdateShippingMethodAsync(ShippingMethodId.From(id), request.Code, request.Name, type, request.Price, request.Currency, request.MinimumOrderAmount, request.MaximumOrderAmount, request.PickupLocationId, request.SortOrder, request.IsEnabled, actor.Value, ct); return x is null ? Results.NotFound() : Results.Ok(Map(x)); }
        catch (TenantScopeViolationException) { return Results.Forbid(); }
        catch (ArgumentException e) { return Validation(e.Message); }
    }

    private static Guid? Actor(HttpContext http) => Guid.TryParse(http.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) && id != Guid.Empty ? id : null;
    private static FulfillmentLocationResponse Map(FulfillmentLocationResult x) => new(x.Id, x.Code, x.Name, x.Phone, x.CountryCode, x.City, x.Region, x.Line1, x.Line2, x.IsDefault, x.IsActive);
    private static ShippingMethodResponse Map(ShippingMethodResult x) => new(x.Id, x.Code, x.Name, x.Type, x.Price, x.Currency, x.MinimumOrderAmount, x.MaximumOrderAmount, x.PickupLocationId, x.SortOrder, x.IsEnabled);
    private static IResult Validation(string message) => Results.BadRequest(new { code = "fulfillment_invalid", message });
}
