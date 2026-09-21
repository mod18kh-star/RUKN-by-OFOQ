using OFOQ.Market.Application.Common.Persistence;
using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Application.Commerce.Carts;
using OFOQ.Market.Application.Commerce.Carts.AddItem;
using OFOQ.Market.Application.Commerce.Carts.ClearCart;
using OFOQ.Market.Application.Commerce.Carts.GetCart;
using OFOQ.Market.Application.Commerce.Carts.RemoveItem;
using OFOQ.Market.Application.Commerce.Carts.UpdateItemQuantity;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Carts;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
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

        group.MapPut(
            "/items/{itemId:guid}",
            UpdateItemQuantityAsync);

        group.MapDelete(
            "/items/{itemId:guid}",
            RemoveItemAsync);

        group.MapDelete(
            "/",
            ClearCartAsync);

        return endpoints;
    }

    private static async Task<IResult> GetCartAsync(
        GetCartHandler handler,
        IStorefrontQueryRepository displayRepository,
        ICurrentTenant currentTenant,
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

                        var mapped = Map(result);

            if (!currentTenant.IsAvailable ||
                !currentTenant.TenantId.HasValue)
            {
                return Results.Forbid();
            }

            var productIds = result.Items
                .Select(item => item.ProductId.Value)
                .Distinct()
                .ToArray();

            var products = await displayRepository
                .GetCartProductsAsync(
                    currentTenant.TenantId.Value,
                    productIds,
                    cancellationToken);

            var lookup = products.ToDictionary(
                product => product.ProductId);

            var enrichedItems = mapped.Items
                .Select(item =>
                {
                    if (!lookup.TryGetValue(
                            item.ProductId,
                            out var product))
                    {
                        return item;
                    }

                    var variant = product.Variants
                        .FirstOrDefault(candidate =>
                            candidate.VariantId ==
                            item.ProductVariantId);

                    decimal? compareAtPrice =
                        product.Currency == item.Currency &&
                        product.CurrentPrice == item.UnitPrice &&
                        product.CompareAtPrice.HasValue &&
                        product.CompareAtPrice.Value > item.UnitPrice
                            ? product.CompareAtPrice
                            : null;

                    return item with
                    {
                        ProductName = product.Name,
                        ProductSlug = product.Slug,
                        VariantName = variant?.Name,
                        PrimaryImageUrl = product.PrimaryImageUrl,
                        PrimaryImageAltText = product.PrimaryImageAltText,
                        CompareAtPrice = compareAtPrice
                    };
                })
                .ToArray();

            return Results.Ok(
                mapped with
                {
                    Items = enrichedItems
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
            return ProductNotAvailable(
                exception.Message);
        }
        catch (CartVariantNotAvailableException exception)
        {
            return VariantNotAvailable(
                exception.Message);
        }
        catch (CartInsufficientStockException exception)
        {
            return InsufficientStock(
                exception.Message);
        }
        catch (StructuredVariantRequiredException exception)
        {
            return StructuredVariantRequired(
                exception.Message);
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

    private static async Task<IResult> UpdateItemQuantityAsync(
        Guid itemId,
        UpdateCartItemQuantityRequest request,
        UpdateCartItemQuantityHandler handler,
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

        if (itemId ==
            Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "invalid_cart_item_id",

                    message =
                        "Cart item ID must be a valid non-empty GUID."
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
                    new UpdateCartItemQuantityCommand(
                        customerUserId.Value,
                        CartItemId.From(
                            itemId),
                        request.Quantity),
                    cancellationToken);

            return Results.Ok(
                Map(result));
        }
        catch (CartNotFoundException exception)
        {
            return CartNotFound(
                exception.Message);
        }
        catch (CartItemNotFoundException exception)
        {
            return CartItemNotFound(
                exception.Message);
        }
        catch (CartProductNotAvailableException exception)
        {
            return ProductNotAvailable(
                exception.Message);
        }
        catch (CartVariantNotAvailableException exception)
        {
            return VariantNotAvailable(
                exception.Message);
        }
        catch (CartInsufficientStockException exception)
        {
            return InsufficientStock(
                exception.Message);
        }
        catch (StructuredVariantRequiredException exception)
        {
            return StructuredVariantRequired(
                exception.Message);
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

    private static async Task<IResult> RemoveItemAsync(
        Guid itemId,
        RemoveCartItemHandler handler,
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

        if (itemId ==
            Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "invalid_cart_item_id",

                    message =
                        "Cart item ID must be a valid non-empty GUID."
                });
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new RemoveCartItemCommand(
                        customerUserId.Value,
                        CartItemId.From(
                            itemId)),
                    cancellationToken);

            return Results.Ok(
                Map(result));
        }
        catch (CartNotFoundException exception)
        {
            return CartNotFound(
                exception.Message);
        }
        catch (CartItemNotFoundException exception)
        {
            return CartItemNotFound(
                exception.Message);
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

    private static async Task<IResult> ClearCartAsync(
        ClearCartHandler handler,
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
                    new ClearCartCommand(
                        customerUserId.Value),
                    cancellationToken);

            /*
             * DELETE /cart is idempotent.
             * No active cart means there was already
             * nothing to clear.
             */
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

    private static IResult CartNotFound(
        string message)
    {
        return Results.NotFound(
            new
            {
                code =
                    "cart_not_found",

                message
            });
    }

    private static IResult CartItemNotFound(
        string message)
    {
        return Results.NotFound(
            new
            {
                code =
                    "cart_item_not_found",

                message
            });
    }

    private static IResult ProductNotAvailable(
        string message)
    {
        return Results.NotFound(
            new
            {
                code =
                    "cart_product_not_available",

                message
            });
    }

    private static IResult VariantNotAvailable(
        string message)
    {
        return Results.NotFound(
            new
            {
                code =
                    "cart_variant_not_available",

                message
            });
    }

    private static IResult InsufficientStock(
        string message)
    {
        return Results.Conflict(
            new
            {
                code =
                    "cart_insufficient_stock",

                message
            });
    }

    private static IResult StructuredVariantRequired(
        string message)
    {
        return Results.BadRequest(
            new
            {
                code =
                    "structured_variant_required",

                message
            });
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