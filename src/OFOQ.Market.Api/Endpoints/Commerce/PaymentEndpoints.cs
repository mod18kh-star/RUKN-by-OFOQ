using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Application.Commerce.Payments.Common;
using OFOQ.Market.Application.Commerce.Payments.CreateIntent;
using OFOQ.Market.Application.Commerce.Payments.GetAvailableMethods;
using OFOQ.Market.Application.Commerce.Payments.GetIntent;
using OFOQ.Market.Application.Commerce.Payments.RetryIntent;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Payments;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class PaymentEndpoints
{
    private const string IdempotencyHeaderName = "Idempotency-Key";

    public static IEndpointRouteBuilder MapPaymentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/tenants/{tenantId:guid}")
            .WithTags("Payments")
            .RequireAuthorization();

        group.MapGet(
            "/orders/{orderId:guid}/payments/methods",
            GetAvailableMethodsAsync);

        group.MapPost(
            "/orders/{orderId:guid}/payments/intents",
            CreateIntentAsync);

        group.MapGet(
            "/payments/intents/{paymentIntentId:guid}",
            GetIntentAsync);

        group.MapPost(
            "/payments/intents/{paymentIntentId:guid}/retry",
            RetryIntentAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAvailableMethodsAsync(
        Guid orderId,
        GetAvailablePaymentMethodsHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var customerUserId = GetUserId(httpContext);

        if (!customerUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        try
        {
            var result = await handler.HandleAsync(
                new GetAvailablePaymentMethodsQuery(
                    OrderId.From(orderId),
                    customerUserId.Value),
                cancellationToken);

            return Results.Ok(
                result.Select(MapMethod).ToArray());
        }
        catch (PaymentOrderNotAvailableException exception)
        {
            return NotFound("payment_order_not_available", exception.Message);
        }
        catch (PaymentAlreadySucceededException exception)
        {
            return Conflict("payment_already_succeeded", exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    private static async Task<IResult> CreateIntentAsync(
        Guid orderId,
        CreatePaymentIntentRequest request,
        CreatePaymentIntentHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var customerUserId = GetUserId(httpContext);

        if (!customerUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var idempotencyKey = GetIdempotencyKey(httpContext);

        if (idempotencyKey is null)
        {
            return IdempotencyRequired();
        }

        try
        {
            var result = await handler.HandleAsync(
                new CreatePaymentIntentCommand(
                    OrderId.From(orderId),
                    customerUserId.Value,
                    TenantPaymentMethodId.From(request.TenantPaymentMethodId),
                    idempotencyKey),
                cancellationToken);

            AddReplayHeader(httpContext, result);

            return Results.Ok(MapIntent(result));
        }
        catch (PaymentOrderNotAvailableException exception)
        {
            return NotFound("payment_order_not_available", exception.Message);
        }
        catch (PaymentMethodNotAvailableException exception)
        {
            return Conflict("payment_method_not_available", exception.Message);
        }
        catch (ElectronicPaymentsSuspendedException exception)
        {
            return Conflict("electronic_payments_suspended", exception.Message);
        }
        catch (PaymentAlreadySucceededException exception)
        {
            return Conflict("payment_already_succeeded", exception.Message);
        }
        catch (PaymentIdempotencyConflictException exception)
        {
            return Conflict("payment_idempotency_conflict", exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    private static async Task<IResult> GetIntentAsync(
        Guid paymentIntentId,
        GetPaymentIntentHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var customerUserId = GetUserId(httpContext);

        if (!customerUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        try
        {
            var result = await handler.HandleAsync(
                new GetPaymentIntentQuery(
                    PaymentIntentId.From(paymentIntentId),
                    customerUserId.Value),
                cancellationToken);

            return Results.Ok(MapIntent(result));
        }
        catch (PaymentIntentNotFoundException exception)
        {
            return NotFound("payment_intent_not_found", exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    private static async Task<IResult> RetryIntentAsync(
        Guid paymentIntentId,
        RetryPaymentIntentHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var customerUserId = GetUserId(httpContext);

        if (!customerUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var idempotencyKey = GetIdempotencyKey(httpContext);

        if (idempotencyKey is null)
        {
            return IdempotencyRequired();
        }

        try
        {
            var result = await handler.HandleAsync(
                new RetryPaymentIntentCommand(
                    PaymentIntentId.From(paymentIntentId),
                    customerUserId.Value,
                    idempotencyKey),
                cancellationToken);

            AddReplayHeader(httpContext, result);

            return Results.Ok(MapIntent(result));
        }
        catch (PaymentIntentNotFoundException exception)
        {
            return NotFound("payment_intent_not_found", exception.Message);
        }
        catch (PaymentIntentNotRetryableException exception)
        {
            return Conflict("payment_intent_not_retryable", exception.Message);
        }
        catch (PaymentMethodNotAvailableException exception)
        {
            return Conflict("payment_method_not_available", exception.Message);
        }
        catch (ElectronicPaymentsSuspendedException exception)
        {
            return Conflict("electronic_payments_suspended", exception.Message);
        }
        catch (PaymentAlreadySucceededException exception)
        {
            return Conflict("payment_already_succeeded", exception.Message);
        }
        catch (PaymentOrderNotAvailableException exception)
        {
            return NotFound("payment_order_not_available", exception.Message);
        }
        catch (PaymentIdempotencyConflictException exception)
        {
            return Conflict("payment_idempotency_conflict", exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    private static UserId? GetUserId(HttpContext httpContext)
    {
        var subject = httpContext.User
            .FindFirst(JwtRegisteredClaimNames.Sub)?
            .Value;

        if (!Guid.TryParse(subject, out var userId) ||
            userId == Guid.Empty)
        {
            return null;
        }

        return UserId.From(userId);
    }

    private static string? GetIdempotencyKey(HttpContext httpContext)
    {
        var value = httpContext.Request.Headers[
            IdempotencyHeaderName].ToString();

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }

    private static void AddReplayHeader(
        HttpContext httpContext,
        PaymentIntentResult result)
    {
        if (result.IsIdempotentReplay)
        {
            httpContext.Response.Headers["Idempotency-Replayed"] = "true";
        }
    }

    private static PaymentMethodResponse MapMethod(PaymentMethodResult result)
    {
        return new PaymentMethodResponse(
            result.Id.Value,
            result.MethodType,
            result.ProviderCode,
            result.DisplayName,
            result.Country,
            result.Currency,
            result.MinimumAmount,
            result.MaximumAmount);
    }

    private static PaymentIntentResponse MapIntent(PaymentIntentResult result)
    {
        return new PaymentIntentResponse(
            result.PaymentIntentId.Value,
            result.PaymentId.Value,
            result.OrderId.Value,
            result.TenantPaymentMethodId.Value,
            result.Status,
            result.MethodType,
            result.ProviderCode,
            result.Amount,
            result.Currency,
            result.ProviderReference,
            result.CreatedAtUtc,
            result.IsIdempotentReplay);
    }

    private static IResult IdempotencyRequired()
    {
        return Results.BadRequest(new
        {
            code = "payment_idempotency_key_required",
            message = $"{IdempotencyHeaderName} header is required."
        });
    }

    private static IResult BadRequest(string message)
    {
        return Results.BadRequest(new
        {
            code = "payment_validation_error",
            message
        });
    }

    private static IResult NotFound(string code, string message)
    {
        return Results.NotFound(new
        {
            code,
            message
        });
    }

    private static IResult Conflict(string code, string message)
    {
        return Results.Conflict(new
        {
            code,
            message
        });
    }
}
