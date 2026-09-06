using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Application.Commerce.Checkout;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Checkout;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class CheckoutEndpoints
{
    private const string IdempotencyHeaderName =
        "Idempotency-Key";

    public static IEndpointRouteBuilder MapCheckoutEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/checkout")
                .WithTags(
                    "Checkout")
                .RequireAuthorization();

        group.MapPost(
            "/",
            CheckoutAsync);

        return endpoints;
    }

    private static async Task<IResult> CheckoutAsync(
        CheckoutHandler handler,
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

        var idempotencyKey =
            httpContext.Request.Headers[
                IdempotencyHeaderName]
                .ToString();

        if (string.IsNullOrWhiteSpace(
                idempotencyKey))
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "checkout_idempotency_key_required",

                    message =
                        $"{IdempotencyHeaderName} header is required."
                });
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new CheckoutCommand(
                        customerUserId.Value,
                        idempotencyKey),
                    cancellationToken);

            if (result.IsIdempotentReplay)
            {
                httpContext.Response.Headers[
                    "Idempotency-Replayed"] =
                    "true";
            }

            return Results.Ok(
                Map(
                    result));
        }
        catch (CheckoutIdempotencyConflictException exception)
        {
            return Conflict(
                "checkout_idempotency_conflict",
                exception.Message);
        }
        catch (CheckoutCartNotFoundException exception)
        {
            return Results.NotFound(
                new
                {
                    code =
                        "checkout_cart_not_found",

                    message =
                        exception.Message
                });
        }
        catch (CheckoutCartEmptyException exception)
        {
            return Conflict(
                "checkout_cart_empty",
                exception.Message);
        }
        catch (CheckoutProductNotAvailableException exception)
        {
            return Conflict(
                "checkout_product_not_available",
                exception.Message);
        }
        catch (CheckoutVariantNotAvailableException exception)
        {
            return Conflict(
                "checkout_variant_not_available",
                exception.Message);
        }
        catch (CheckoutStructuredVariantInvalidException exception)
        {
            return Conflict(
                "checkout_structured_variant_invalid",
                exception.Message);
        }
        catch (CheckoutInsufficientStockException exception)
        {
            return Conflict(
                "checkout_insufficient_stock",
                exception.Message);
        }
        catch (CheckoutCurrencyChangedException exception)
        {
            return Conflict(
                "checkout_currency_changed",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "checkout_validation_error",

                    message =
                        exception.Message
                });
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

    private static CheckoutResponse Map(
        CheckoutResult result)
    {
        return new CheckoutResponse(
            result.OrderId.Value,
            result.SourceCartId.Value,
            result.Status,
            result.Currency,
            result.TotalQuantity,
            result.TotalAmount,
            result.CreatedAtUtc,
            result.IsIdempotentReplay,
            result.Items
                .Select(
                    item =>
                        new CheckoutItemResponse(
                            item.Id.Value,
                            item.ProductId.Value,
                            item.ProductVariantId.Value,
                            item.ProductName,
                            item.VariantName,
                            item.Sku,
                            item.UnitPrice,
                            item.Currency,
                            item.Quantity,
                            item.LineTotal))
                .ToArray());
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
