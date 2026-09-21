using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Application.Commerce.Checkout;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Fulfillment;
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

        group.MapPost("/quote", QuoteAsync);

        return endpoints;
    }

    public sealed record CheckoutQuoteRequest(Guid ShippingMethodId, string? CouponCode);

    private sealed record CheckoutQuoteResponse(
        string CouponCode,
        string Currency,
        decimal SubtotalAmount,
        decimal ShippingAmount,
        decimal DiscountAmount,
        decimal TotalAmount);

    // Preview only. Does not create an order or consume a coupon use.
    // Existing checkout rechecks pricing, currency, inventory and limits atomically.
    private static async Task<IResult> QuoteAsync(
        CheckoutQuoteRequest request,
        ICartRepository carts,
        IProductRepository products,
        IProductVariantRepository variants,
        CheckoutPricingService pricing,
        ITransactionExecutor transactions,
        HttpContext http,
        CancellationToken ct)
    {
        var userId = GetUserId(http);
        if (!userId.HasValue) return Results.Unauthorized();
        if (request is null || request.ShippingMethodId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.CouponCode) || request.CouponCode.Length > 60 ||
            !System.Text.RegularExpressions.Regex.IsMatch(request.CouponCode.Trim(), @"^[A-Za-z0-9][A-Za-z0-9_-]{1,59}$"))
            return Results.BadRequest(new { code = "checkout_coupon_invalid", message = "Enter a valid coupon and shipping method." });

        try
        {
            var quote = await transactions.ExecuteAsync(async transactionCt =>
            {
                var cart = await carts.GetActiveByCustomerUserIdAsync(userId.Value, transactionCt);
                if (cart is null || cart.Items.Count == 0)
                    throw new CheckoutPricingException("checkout_cart_empty", "Your cart is empty.");

                var items = new List<CheckoutPricingItem>(cart.Items.Count);
                CurrencyCode? currency = null;
                foreach (var item in cart.Items)
                {
                    var product = await products.GetByIdAsync(item.ProductId, transactionCt);
                    var variant = await variants.GetByIdAsync(item.ProductVariantId, transactionCt);
                    if (product is null || product.Status != ProductStatus.Published || !product.IsVisible ||
                        variant is null || !variant.IsEnabled || variant.ProductId != product.Id)
                        throw new CheckoutPricingException("checkout_product_unavailable", "A product in the cart is unavailable.");
                    var price = variant.PriceOverride ?? product.Price;
                    if (currency.HasValue && currency.Value != price.Currency)
                        throw new CheckoutPricingException("checkout_currency_changed", "Product currencies do not match.");
                    currency = price.Currency;
                    items.Add(new CheckoutPricingItem(product.Id, price.Amount * item.Quantity));
                }
                if (!currency.HasValue)
                    throw new CheckoutPricingException("checkout_cart_empty", "Your cart is empty.");
                var subtotal = decimal.Round(items.Sum(i => i.LineTotal), 2, MidpointRounding.AwayFromZero);
                var calculation = await pricing.ResolveAsync(userId.Value, null,
                    ShippingMethodId.From(request.ShippingMethodId), request.CouponCode,
                    items, currency.Value, transactionCt);
                var total = subtotal + calculation.ShippingAmount - calculation.DiscountAmount;
                if (total <= 0m)
                    throw new CheckoutPricingException("checkout_total_nonpositive", "This coupon reduces the total to zero. Manual payment cannot process a zero-value order.");
                return new CheckoutQuoteResponse(calculation.CouponCode!, currency.Value.Value,
                    subtotal, calculation.ShippingAmount, calculation.DiscountAmount, total);
            }, ct);
            return Results.Ok(quote);
        }
        catch (CheckoutPricingException e) { return Conflict(e.Code, e.Message); }
        catch (TenantScopeViolationException) { return Results.Forbid(); }
        catch (ArgumentException) { return Results.BadRequest(new { code = "checkout_coupon_invalid", message = "Invalid coupon or shipping method." }); }
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

        CheckoutRequest? request = null;

        if (httpContext.Request.ContentLength is > 0)
        {
            request =
                await httpContext.Request.ReadFromJsonAsync<CheckoutRequest>(
                    cancellationToken);
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new CheckoutCommand(
                        customerUserId.Value,
                        idempotencyKey,
                        request?.CustomerAddressId is { } addressId
                            ? OFOQ.Market.Domain.Commerce.Customers.CustomerAddressId.From(addressId)
                            : null,
                        request?.ShippingMethodId is { } shippingMethodId
                            ? OFOQ.Market.Domain.Commerce.Fulfillment.ShippingMethodId.From(shippingMethodId)
                            : null,
                        request?.CouponCode),
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
        catch (CheckoutPricingException exception)
        {
            return Conflict(
                exception.Code,
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
            result.SubtotalAmount,
            result.ShippingAmount,
            result.DiscountAmount,
            result.TotalAmount,
            result.AppliedCouponCode,
            result.ShippingMethodName,
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
