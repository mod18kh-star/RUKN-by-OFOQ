using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Application.Commerce.Customers;
using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Identity;

public sealed record CustomerSavedAddressRequest(
    string Label,
    string RecipientName,
    string Phone,
    string CountryCode,
    string? Region,
    string City,
    string? PostalCode,
    string Line1,
    string? Line2,
    decimal? Latitude,
    decimal? Longitude,
    string? MapUrl,
    string? DeliveryNotes,
    bool IsDefault
);

public static class CustomerSavedAddressEndpoints
{
    public static IEndpointRouteBuilder
        MapCustomerSavedAddressEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/customer/addresses")
            .WithTags("Customer Saved Addresses")
            .RequireAuthorization();

        group.MapGet(
            "",
            GetAddressesAsync);

        group.MapPost(
            "",
            AddAddressAsync);

        group.MapPut(
            "/{addressId:guid}",
            UpdateAddressAsync);

        group.MapDelete(
            "/{addressId:guid}",
            DeleteAddressAsync);

        group.MapPut(
            "/{addressId:guid}/default",
            SetDefaultAsync);

        return endpoints;
    }

    private static async Task<IResult>
        GetAddressesAsync(
            CustomerSavedAddressService service,
            HttpContext http,
            CancellationToken ct)
    {
        var userId = GetUserId(http);

        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var addresses = await service.GetAddressesAsync(
            userId.Value,
            ct);

        return Results.Ok(
            addresses.Select(Map).ToArray());
    }

    private static async Task<IResult>
        AddAddressAsync(
            CustomerSavedAddressRequest request,
            CustomerSavedAddressService service,
            HttpContext http,
            CancellationToken ct)
    {
        var userId = GetUserId(http);

        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        try
        {
            var saved = await service.AddAddressAsync(
                userId.Value,
                ToDetails(request),
                request.IsDefault,
                ct);

            return Results.Created(
                $"/api/customer/addresses/{saved.Id}",
                Map(saved));
        }
        catch (ArgumentException)
        {
            return InvalidAddress();
        }
    }

    private static async Task<IResult>
        UpdateAddressAsync(
            Guid addressId,
            CustomerSavedAddressRequest request,
            CustomerSavedAddressService service,
            HttpContext http,
            CancellationToken ct)
    {
        var userId = GetUserId(http);

        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (addressId == Guid.Empty)
        {
            return Results.NotFound();
        }

        try
        {
            var address = await service.UpdateAddressAsync(
                userId.Value,
                addressId,
                ToDetails(request),
                request.IsDefault,
                ct);

            return address is null
                ? Results.NotFound()
                : Results.Ok(Map(address));
        }
        catch (ArgumentException)
        {
            return InvalidAddress();
        }
    }

    private static async Task<IResult>
        DeleteAddressAsync(
            Guid addressId,
            CustomerSavedAddressService service,
            HttpContext http,
            CancellationToken ct)
    {
        var userId = GetUserId(http);

        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (addressId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var deleted = await service.DeleteAddressAsync(
            userId.Value,
            addressId,
            ct);

        return deleted
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult>
        SetDefaultAsync(
            Guid addressId,
            CustomerSavedAddressService service,
            HttpContext http,
            CancellationToken ct)
    {
        var userId = GetUserId(http);

        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (addressId == Guid.Empty)
        {
            return Results.NotFound();
        }

        var address = await service.SetDefaultAsync(
            userId.Value,
            addressId,
            ct);

        return address is null
            ? Results.NotFound()
            : Results.Ok(Map(address));
    }

    private static SavedAddressDetails ToDetails(
        CustomerSavedAddressRequest request)
    {
        return new SavedAddressDetails(
            request.Label,
            request.RecipientName,
            request.Phone,
            request.CountryCode,
            request.Region,
            request.City,
            request.PostalCode,
            request.Line1,
            request.Line2,
            request.Latitude,
            request.Longitude,
            request.MapUrl,
            request.DeliveryNotes);
    }

    private static UserId? GetUserId(
        HttpContext http)
    {
        http.Response.Headers["Cache-Control"] =
            "no-store";

        var subject = http.User
            .FindFirst(JwtRegisteredClaimNames.Sub)
            ?.Value;

        if (
            !Guid.TryParse(subject, out var id) ||
            id == Guid.Empty
        )
        {
            return null;
        }

        return UserId.From(id);
    }

    private static IResult InvalidAddress()
    {
        return Results.BadRequest(new
        {
            code = "customer_address_invalid",
            message = "Check the address information and try again."
        });
    }

    private static object Map(
        CustomerSavedAddress address)
    {
        return new
        {
            address.Id,
            UserId = address.UserId.Value,
            address.Label,
            address.RecipientName,
            address.Phone,
            address.CountryCode,
            address.Region,
            address.City,
            address.PostalCode,
            address.Line1,
            address.Line2,
            address.Latitude,
            address.Longitude,
            address.MapUrl,
            address.DeliveryNotes,
            address.IsDefault,
            address.CreatedAtUtc,
            address.UpdatedAtUtc
        };
    }
}