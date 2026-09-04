using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Application.Commerce.Carts;
using OFOQ.Market.Application.Commerce.Carts.AddItem;
using OFOQ.Market.Application.Commerce.Carts.GetCart;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Carts;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class CartEndpoints
{
    public static IEndpointRouteBuilder MapCartEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/cart")
                .WithTags(
                    "Cart")
                .RequireAuthorization();

        group.MapGet(
            "/",
            GetCartAsync);

        group.MapPost(
            "/items",
            AddItemAsync);

        return endpoints;
    }

    private static async Task<IResult> GetCartAsync(
        GetCartHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var customerUserId =
            GetUserId(
                httpContext);

        if (!customerUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    customerUserId.Value,
                    cancellationToken);

            if (result is null)
            {
                return Results.NoContent();
            }

            return Results.Ok(
                Map(result));
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

    private static async Task<IResult> AddItemAsync(
        AddToCartRequest request,
        AddToCartHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var customerUserId =
            GetUserId(
                httpContext);

        if (!customerUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (request.ProductId ==
            Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "invalid_product_id",

                    message =
                        "Product ID must be a valid non-empty GUID."
                });
        }

        if (request.ProductVariantId ==
            Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "invalid_product_variant_id",

                    message =
                        "Product variant ID must be a valid non-empty GUID."
                });
        }

        if (request.Quantity <= 0)
        {
            return ValidationError(
                "Quantity must be greater than zero.");
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new AddToCartCommand(
                        customerUserId.Value,
                        ProductId.From(
                            request.ProductId),
                        ProductVariantId.From(
                            request.ProductVariantId),
                        request.Quantity),
                    cancellationToken);

            return Results.Ok(
                Map(result));
        }
        catch (CartProductNotAvailableException exception)
        {
            return Results.NotFound(
                new
                {
                    code =
                        "cart_product_not_available",

                    message =
                        exception.Message
                });
        }
        catch (CartVariantNotAvailableException exception)
        {
            return Results.NotFound(
                new
                {
                    code =
                        "cart_variant_not_available",

                    message =
                        exception.Message
                });
        }
        catch (CartInsufficientStockException exception)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "cart_insufficient_stock",

                    message =
                        exception.Message
                });
        }
        catch (StructuredVariantRequiredException exception)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "structured_variant_required",

                    message =
                        exception.Message
                });
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

    private static UserId? GetUserId(
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

        return UserId.From(
            userId);
    }

    private static CartResponse Map(
        CartResult result)
    {
        return new CartResponse(
            result.Id.Value,
            result.Currency,
            result.TotalQuantity,
            result.TotalAmount,
            result.Items
                .Select(
                    item =>
                        new CartItemResponse(
                            item.Id.Value,
                            item.ProductId.Value,
                            item.ProductVariantId.Value,
                            item.UnitPrice,
                            item.Currency,
                            item.Quantity,
                            item.LineTotal))
                .ToArray());
    }

    private static IResult ValidationError(
        string message)
    {
        return Results.BadRequest(
            new
            {
                code =
                    "validation_error",

                message
            });
    }
}