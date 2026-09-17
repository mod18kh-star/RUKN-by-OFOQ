using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Application.Commerce.Customers;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Customers;
using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class CustomerAccountEndpoints
{
    public static IEndpointRouteBuilder MapCustomerAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/tenants/{tenantId:guid}/account").WithTags("Customer Account").RequireAuthorization();
        group.MapGet("/profile", GetProfileAsync);
        group.MapPut("/profile", UpdateProfileAsync);
        group.MapGet("/addresses", GetAddressesAsync);
        group.MapPost("/addresses", AddAddressAsync);
        group.MapPut("/addresses/{addressId:guid}", UpdateAddressAsync);
        group.MapDelete("/addresses/{addressId:guid}", DeleteAddressAsync);
        return endpoints;
    }

    private static async Task<IResult> GetProfileAsync(CustomerAccountService service, HttpContext http, CancellationToken ct)
    {
        var userId = GetUserId(http); if (!userId.HasValue) return Results.Unauthorized();
        try { var x = await service.GetOrCreateProfileAsync(userId.Value, ct); return Results.Ok(new CustomerProfileResponse(x.UserId, x.DisplayName, x.Phone, x.IsBlocked)); }
        catch (TenantScopeViolationException) { return Results.Forbid(); }
    }

    private static async Task<IResult> UpdateProfileAsync(UpdateCustomerProfileRequest request, CustomerAccountService service, HttpContext http, CancellationToken ct)
    {
        var userId = GetUserId(http); if (!userId.HasValue) return Results.Unauthorized();
        try { var x = await service.UpdateProfileAsync(userId.Value, request.DisplayName, request.Phone, ct); return Results.Ok(new CustomerProfileResponse(x.UserId, x.DisplayName, x.Phone, x.IsBlocked)); }
        catch (TenantScopeViolationException) { return Results.Forbid(); }
        catch (ArgumentException e) { return Validation(e.Message); }
    }

    private static async Task<IResult> GetAddressesAsync(CustomerAccountService service, HttpContext http, CancellationToken ct)
    {
        var userId = GetUserId(http); if (!userId.HasValue) return Results.Unauthorized();
        try { return Results.Ok((await service.GetAddressesAsync(userId.Value, ct)).Select(Map).ToArray()); }
        catch (TenantScopeViolationException) { return Results.Forbid(); }
    }

    private static async Task<IResult> AddAddressAsync(CustomerAddressRequest request, CustomerAccountService service, HttpContext http, CancellationToken ct)
    {
        var userId = GetUserId(http); if (!userId.HasValue) return Results.Unauthorized();
        try { return Results.Ok(Map(await service.AddAddressAsync(userId.Value, request.Label, request.RecipientName, request.Phone, request.CountryCode, request.Region, request.City, request.PostalCode, request.Line1, request.Line2, request.IsDefault, ct))); }
        catch (TenantScopeViolationException) { return Results.Forbid(); }
        catch (ArgumentException e) { return Validation(e.Message); }
    }

    private static async Task<IResult> UpdateAddressAsync(Guid addressId, CustomerAddressRequest request, CustomerAccountService service, HttpContext http, CancellationToken ct)
    {
        var userId = GetUserId(http); if (!userId.HasValue) return Results.Unauthorized();
        try { var x = await service.UpdateAddressAsync(userId.Value, CustomerAddressId.From(addressId), request.Label, request.RecipientName, request.Phone, request.CountryCode, request.Region, request.City, request.PostalCode, request.Line1, request.Line2, request.IsDefault, ct); return x is null ? Results.NotFound() : Results.Ok(Map(x)); }
        catch (TenantScopeViolationException) { return Results.Forbid(); }
        catch (ArgumentException e) { return Validation(e.Message); }
    }

    private static async Task<IResult> DeleteAddressAsync(Guid addressId, CustomerAccountService service, HttpContext http, CancellationToken ct)
    {
        var userId = GetUserId(http); if (!userId.HasValue) return Results.Unauthorized();
        try { return await service.DeleteAddressAsync(userId.Value, CustomerAddressId.From(addressId), ct) ? Results.NoContent() : Results.NotFound(); }
        catch (TenantScopeViolationException) { return Results.Forbid(); }
        catch (ArgumentException e) { return Validation(e.Message); }
    }

    private static UserId? GetUserId(HttpContext http)
    {
        var raw = http.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(raw, out var id) && id != Guid.Empty ? UserId.From(id) : null;
    }

    private static CustomerAddressResponse Map(CustomerAddressResult x) => new(x.AddressId, x.Label, x.RecipientName, x.Phone, x.CountryCode, x.Region, x.City, x.PostalCode, x.Line1, x.Line2, x.IsDefault, x.IsActive);
    private static IResult Validation(string message) => Results.BadRequest(new { code = "customer_account_invalid", message });
}
