using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Infrastructure.Persistence;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Commerce.Payments.Common;
using OFOQ.Market.Application.Commerce.Payments.CreateIntent;
using OFOQ.Market.Application.Commerce.Payments.ExecuteIntent;
using OFOQ.Market.Application.Commerce.Payments.GetAvailableMethods;
using OFOQ.Market.Application.Commerce.Payments.GetIntent;
using OFOQ.Market.Application.Commerce.Payments.ProcessWebhook;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Common;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Create;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Credentials;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Get;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.State;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Wallets;
using OFOQ.Market.Application.Commerce.Payments.RetryIntent;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Payments;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class PaymentEndpoints
{
    private const string IdempotencyHeaderName =
        "Idempotency-Key";

    public static IEndpointRouteBuilder MapPaymentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}")
                .WithTags(
                    "Payments")
                .RequireAuthorization();

        // -------------------------------------------------
        // Customer payment flow
        // -------------------------------------------------

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

        group.MapPost(
            "/payments/intents/{paymentIntentId:guid}/execute",
            ExecuteIntentAsync);

        // -------------------------------------------------
        // Provider accounts / privileged tenant configuration
        // -------------------------------------------------

        var providerAccounts =
            group
                .MapGroup(
                    "/payments/provider-accounts")
                .WithTags(
                    "Payment Provider Accounts")
                .RequireAuthorization(
                    AuthorizationPolicies
                        .TenantPaymentAdministration);

        providerAccounts.MapGet(
            "/",
            GetProviderAccountsAsync);

        providerAccounts.MapPost(
            "/",
            CreateProviderAccountAsync);

        providerAccounts.MapPut(
            "/{accountId:guid}/credentials",
            UpdateProviderCredentialsAsync);

        providerAccounts.MapPut(
            "/{accountId:guid}/state",
            SetProviderAccountStateAsync);

        providerAccounts.MapPut(
            "/{accountId:guid}/wallets",
            SetWalletCapabilityAsync);

        // -------------------------------------------------
        // Provider webhooks
        // -------------------------------------------------

        group.MapPost(
                "/payments/webhooks/{tenantPaymentMethodId:guid}",
                ProcessWebhookAsync)
            .AllowAnonymous();

        endpoints.MapMerchantManualPaymentEndpoints();
        endpoints.MapManualOrderPaymentEndpoints();

        return endpoints;
    }

    // =================================================
    // Customer payment methods
    // =================================================

    private static async Task<IResult> GetAvailableMethodsAsync(
        Guid orderId,
        GetAvailablePaymentMethodsHandler handler,
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
                    new GetAvailablePaymentMethodsQuery(
                        OrderId.From(
                            orderId),
                        customerUserId.Value),
                    cancellationToken);

            return Results.Ok(
                result
                    .Select(
                        MapMethod)
                    .ToArray());
        }
        catch (PaymentOrderNotAvailableException exception)
        {
            return NotFound(
                "payment_order_not_available",
                exception.Message);
        }
        catch (PaymentAlreadySucceededException exception)
        {
            return Conflict(
                "payment_already_succeeded",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                exception.Message);
        }
    }

    // =================================================
    // Create payment intent
    // =================================================

    private static async Task<IResult> CreateIntentAsync(
        Guid orderId,
        CreatePaymentIntentRequest request,
        CreatePaymentIntentHandler handler,
        IManualOrderPaymentSelectionReader manualPaymentSelection,
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

        // A manual transfer selection excludes electronic payment intents for this order.
        if (orderId != Guid.Empty && await manualPaymentSelection
            .HasManualPaymentAsync(orderId, cancellationToken))
        {
            return Conflict("manual_payment_selected", "This order uses a manual transfer.");
        }

        var idempotencyKey =
            GetIdempotencyKey(
                httpContext);

        if (idempotencyKey is null)
        {
            return IdempotencyRequired();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new CreatePaymentIntentCommand(
                        OrderId.From(
                            orderId),
                        customerUserId.Value,
                        TenantPaymentMethodId.From(
                            request.TenantPaymentMethodId),
                        idempotencyKey),
                    cancellationToken);

            AddReplayHeader(
                httpContext,
                result);

            return Results.Ok(
                MapIntent(
                    result));
        }
        catch (PaymentOrderNotAvailableException exception)
        {
            return NotFound(
                "payment_order_not_available",
                exception.Message);
        }
        catch (PaymentMethodNotAvailableException exception)
        {
            return Conflict(
                "payment_method_not_available",
                exception.Message);
        }
        catch (ElectronicPaymentsSuspendedException exception)
        {
            return Conflict(
                "electronic_payments_suspended",
                exception.Message);
        }
        catch (PaymentAlreadySucceededException exception)
        {
            return Conflict(
                "payment_already_succeeded",
                exception.Message);
        }
        catch (PaymentIdempotencyConflictException exception)
        {
            return Conflict(
                "payment_idempotency_conflict",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                exception.Message);
        }
    }

    // =================================================
    // Get intent
    // =================================================

    private static async Task<IResult> GetIntentAsync(
        Guid paymentIntentId,
        GetPaymentIntentHandler handler,
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
                    new GetPaymentIntentQuery(
                        PaymentIntentId.From(
                            paymentIntentId),
                        customerUserId.Value),
                    cancellationToken);

            return Results.Ok(
                MapIntent(
                    result));
        }
        catch (PaymentIntentNotFoundException exception)
        {
            return NotFound(
                "payment_intent_not_found",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                exception.Message);
        }
    }

    // =================================================
    // Retry intent
    // =================================================

    private static async Task<IResult> RetryIntentAsync(
        Guid paymentIntentId,
        RetryPaymentIntentHandler handler,
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
            GetIdempotencyKey(
                httpContext);

        if (idempotencyKey is null)
        {
            return IdempotencyRequired();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new RetryPaymentIntentCommand(
                        PaymentIntentId.From(
                            paymentIntentId),
                        customerUserId.Value,
                        idempotencyKey),
                    cancellationToken);

            AddReplayHeader(
                httpContext,
                result);

            return Results.Ok(
                MapIntent(
                    result));
        }
        catch (PaymentIntentNotFoundException exception)
        {
            return NotFound(
                "payment_intent_not_found",
                exception.Message);
        }
        catch (PaymentIntentNotRetryableException exception)
        {
            return Conflict(
                "payment_intent_not_retryable",
                exception.Message);
        }
        catch (PaymentMethodNotAvailableException exception)
        {
            return Conflict(
                "payment_method_not_available",
                exception.Message);
        }
        catch (ElectronicPaymentsSuspendedException exception)
        {
            return Conflict(
                "electronic_payments_suspended",
                exception.Message);
        }
        catch (PaymentAlreadySucceededException exception)
        {
            return Conflict(
                "payment_already_succeeded",
                exception.Message);
        }
        catch (PaymentOrderNotAvailableException exception)
        {
            return NotFound(
                "payment_order_not_available",
                exception.Message);
        }
        catch (PaymentIdempotencyConflictException exception)
        {
            return Conflict(
                "payment_idempotency_conflict",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                exception.Message);
        }
    }

    // =================================================
    // Execute intent
    // =================================================

    private static async Task<IResult> ExecuteIntentAsync(
        Guid paymentIntentId,
        ExecutePaymentIntentHandler handler,
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
                    new ExecutePaymentIntentCommand(
                        PaymentIntentId.From(
                            paymentIntentId),
                        customerUserId.Value),
                    cancellationToken);

            return Results.Ok(
                MapIntent(
                    result));
        }
        catch (PaymentIntentNotFoundException exception)
        {
            return NotFound(
                "payment_intent_not_found",
                exception.Message);
        }
        catch (PaymentIntentNotExecutableException exception)
        {
            return Conflict(
                "payment_intent_not_executable",
                exception.Message);
        }
        catch (PaymentAttemptAlreadyActiveException exception)
        {
            return Conflict(
                "payment_attempt_already_active",
                exception.Message);
        }
        catch (PaymentMethodNotAvailableException exception)
        {
            return Conflict(
                "payment_method_not_available",
                exception.Message);
        }
        catch (ElectronicPaymentsSuspendedException exception)
        {
            return Conflict(
                "electronic_payments_suspended",
                exception.Message);
        }
        catch (PaymentAlreadySucceededException exception)
        {
            return Conflict(
                "payment_already_succeeded",
                exception.Message);
        }
        catch (PaymentOrderNotAvailableException exception)
        {
            return NotFound(
                "payment_order_not_available",
                exception.Message);
        }
        catch (PaymentProviderNotConfiguredException exception)
        {
            return BadGateway(
                "payment_provider_not_configured",
                exception.Message);
        }
        catch (PaymentProviderUnavailableException exception)
        {
            return BadGateway(
                "payment_provider_unavailable",
                exception.Message);
        }
        catch (PaymentProviderResultInvalidException exception)
        {
            return BadGateway(
                "payment_provider_result_invalid",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                exception.Message);
        }
    }

    // =================================================
    // Provider accounts
    // =================================================

    private static async Task<IResult> GetProviderAccountsAsync(
        GetPaymentProviderAccountsHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actorUserId =
            GetUserId(
                httpContext);

        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new GetPaymentProviderAccountsQuery(),
                    cancellationToken);

            return Results.Ok(
                result
                    .Select(
                        MapProviderAccount)
                    .ToArray());
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                exception.Message);
        }
    }

    private static async Task<IResult> CreateProviderAccountAsync(
        CreatePaymentProviderAccountRequest request,
        CreatePaymentProviderAccountHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actorUserId =
            GetUserId(
                httpContext);

        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryParseProviderEnvironment(
                request.Environment,
                out var environment))
        {
            return BadRequest(
                "payment_provider_environment_invalid",
                "Payment provider environment must be Sandbox or Production.");
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new CreatePaymentProviderAccountCommand(
                        request.ProviderCode,
                        request.DisplayName,
                        environment,
                        actorUserId.Value.Value),
                    cancellationToken);

            var tenantId =
                httpContext.Request.RouteValues[
                    "tenantId"];

            return Results.Created(
                $"/api/tenants/{tenantId}/payments/provider-accounts/{result.AccountId}",
                MapProviderAccount(
                    result));
        }
        catch (PaymentProviderAccountAlreadyExistsException exception)
        {
            return Conflict(
                "payment_provider_account_already_exists",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                exception.Message);
        }
    }

    private static async Task<IResult> UpdateProviderCredentialsAsync(
        Guid accountId,
        UpdatePaymentProviderCredentialsRequest request,
        UpdatePaymentProviderCredentialsHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actorUserId =
            GetUserId(
                httpContext);

        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new UpdatePaymentProviderCredentialsCommand(
                        TenantPaymentProviderAccountId.From(
                            accountId),
                        request.Credentials,
                        actorUserId.Value.Value),
                    cancellationToken);

            return Results.Ok(
                MapProviderAccount(
                    result));
        }
        catch (PaymentProviderAccountNotFoundException exception)
        {
            return NotFound(
                "payment_provider_account_not_found",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                exception.Message);
        }
    }

    private static async Task<IResult> SetProviderAccountStateAsync(
        Guid accountId,
        SetPaymentProviderAccountStateRequest request,
        SetPaymentProviderAccountStateHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actorUserId =
            GetUserId(
                httpContext);

        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new SetPaymentProviderAccountStateCommand(
                        TenantPaymentProviderAccountId.From(
                            accountId),
                        request.Enabled,
                        actorUserId.Value.Value),
                    cancellationToken);

            return Results.Ok(
                MapProviderAccount(
                    result));
        }
        catch (PaymentProviderAccountNotFoundException exception)
        {
            return NotFound(
                "payment_provider_account_not_found",
                exception.Message);
        }
        catch (PaymentProviderAccountAlreadyExistsException exception)
        {
            return Conflict(
                "payment_provider_account_already_enabled",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(
                "payment_provider_account_cannot_be_enabled",
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                exception.Message);
        }
    }

    private static async Task<IResult> SetWalletCapabilityAsync(
        Guid accountId,
        SetPaymentWalletCapabilityRequest request,
        SetPaymentWalletCapabilityHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actorUserId =
            GetUserId(
                httpContext);

        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryParseWalletType(
                request.WalletType,
                out var walletType))
        {
            return BadRequest(
                "payment_wallet_type_invalid",
                "Supported wallet types are ApplePay and SamsungPay.");
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new SetPaymentWalletCapabilityCommand(
                        TenantPaymentProviderAccountId.From(
                            accountId),
                        walletType,
                        request.Enabled,
                        actorUserId.Value.Value),
                    cancellationToken);

            return Results.Ok(
                MapWalletCapability(
                    result));
        }
        catch (PaymentProviderAccountNotFoundException exception)
        {
            return NotFound(
                "payment_provider_account_not_found",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                exception.Message);
        }
    }

    // =================================================
    // Webhook
    // =================================================

    private static async Task<IResult> ProcessWebhookAsync(
        Guid tenantPaymentMethodId,
        ProcessPaymentWebhookHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            byte[] rawBody;

            using (var stream =
                   new MemoryStream())
            {
                await httpContext.Request.Body.CopyToAsync(
                    stream,
                    cancellationToken);

                rawBody =
                    stream.ToArray();
            }

            var headers =
                httpContext.Request.Headers
                    .ToDictionary(
                        header =>
                            header.Key,
                        header =>
                            header.Value.ToString(),
                        StringComparer.OrdinalIgnoreCase);

            var result =
                await handler.HandleAsync(
                    new ProcessPaymentWebhookCommand(
                        TenantPaymentMethodId.From(
                            tenantPaymentMethodId),
                        rawBody,
                        headers),
                    cancellationToken);

            return Results.Ok(
                new PaymentWebhookResponse(
                    result.PaymentIntentId.Value,
                    result.PaymentId.Value,
                    result.OrderId.Value,
                    result.Status,
                    result.Duplicate,
                    result.Applied));
        }
        catch (PaymentWebhookSignatureInvalidException)
        {
            return Results.Unauthorized();
        }
        catch (PaymentWebhookAmountMismatchException exception)
        {
            return Conflict(
                "payment_webhook_amount_mismatch",
                exception.Message);
        }
        catch (PaymentWebhookPayloadInvalidException exception)
        {
            return BadRequest(
                "payment_webhook_payload_invalid",
                exception.Message);
        }
        catch (PaymentWebhookTargetNotFoundException exception)
        {
            return NotFound(
                "payment_webhook_target_not_found",
                exception.Message);
        }
        catch (PaymentProviderNotConfiguredException exception)
        {
            return BadGateway(
                "payment_provider_not_configured",
                exception.Message);
        }
        catch (PaymentProviderResultInvalidException exception)
        {
            return BadGateway(
                "payment_provider_result_invalid",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                exception.Message);
        }
    }

    // =================================================
    // Authentication helpers
    // =================================================

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
            userId == Guid.Empty)
        {
            return null;
        }

        return UserId.From(
            userId);
    }

    // =================================================
    // Parsing helpers
    // =================================================

    private static bool TryParseProviderEnvironment(
        string? value,
        out PaymentProviderEnvironment environment)
    {
        environment =
            default;

        if (string.IsNullOrWhiteSpace(
                value))
        {
            return false;
        }

        if (!Enum.TryParse(
                value.Trim(),
                ignoreCase: true,
                out environment))
        {
            return false;
        }

        return environment is
            PaymentProviderEnvironment.Sandbox or
            PaymentProviderEnvironment.Production;
    }

    private static bool TryParseWalletType(
        string? value,
        out PaymentWalletType walletType)
    {
        walletType =
            PaymentWalletType.Unknown;

        if (string.IsNullOrWhiteSpace(
                value))
        {
            return false;
        }

        if (!Enum.TryParse(
                value.Trim(),
                ignoreCase: true,
                out walletType))
        {
            return false;
        }

        return walletType is
            PaymentWalletType.ApplePay or
            PaymentWalletType.SamsungPay;
    }

    // =================================================
    // Idempotency
    // =================================================

    private static string? GetIdempotencyKey(
        HttpContext httpContext)
    {
        var value =
            httpContext.Request.Headers[
                    IdempotencyHeaderName]
                .ToString();

        return string.IsNullOrWhiteSpace(
            value)
            ? null
            : value;
    }

    private static void AddReplayHeader(
        HttpContext httpContext,
        PaymentIntentResult result)
    {
        if (result.IsIdempotentReplay)
        {
            httpContext.Response.Headers[
                    "Idempotency-Replayed"] =
                "true";
        }
    }

    // =================================================
    // Response mapping
    // =================================================

    private static PaymentMethodResponse MapMethod(
        PaymentMethodResult result)
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

    private static PaymentIntentResponse MapIntent(
        PaymentIntentResult result)
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
            result.IsIdempotentReplay)
        {
            ActionType =
                result.ActionType?.ToString(),

            ActionValue =
                result.ActionValue
        };
    }

    private static PaymentProviderAccountResponse MapProviderAccount(
        PaymentProviderAccountResult result)
    {
        return new PaymentProviderAccountResponse(
            result.AccountId,
            result.ProviderCode,
            result.DisplayName,
            result.Environment,
            result.IsEnabled,
            result.HasCredentials,
            result.CredentialsVersion,
            result.CreatedAtUtc,
            result.Wallets
                .Select(
                    MapWalletCapability)
                .ToArray());
    }

    private static PaymentWalletCapabilityResponse MapWalletCapability(
        PaymentWalletCapabilityResult result)
    {
        return new PaymentWalletCapabilityResponse(
            result.CapabilityId,
            result.WalletType,
            result.IsEnabled);
    }

    // =================================================
    // HTTP errors
    // =================================================

    private static IResult IdempotencyRequired()
    {
        return Results.BadRequest(
            new
            {
                code =
                    "payment_idempotency_key_required",

                message =
                    $"{IdempotencyHeaderName} header is required."
            });
    }

    private static IResult BadRequest(
        string message)
    {
        return Results.BadRequest(
            new
            {
                code =
                    "payment_validation_error",

                message
            });
    }

    private static IResult BadRequest(
        string code,
        string message)
    {
        return Results.BadRequest(
            new
            {
                code,
                message
            });
    }

    private static IResult NotFound(
        string code,
        string message)
    {
        return Results.NotFound(
            new
            {
                code,
                message
            });
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

    private static IResult BadGateway(
        string code,
        string message)
    {
        return Results.Json(
            new
            {
                code,
                message
            },
            statusCode:
                StatusCodes.Status502BadGateway);
    }
}

// The manual transfer guard is injectable so API tests can use the existing
// in-memory repositories without opening the real PostgreSQL connection.
// Never disable this guard in the production application.
public interface IManualOrderPaymentSelectionReader
{
    Task<bool> HasManualPaymentAsync(Guid orderId, CancellationToken cancellationToken);
}

internal sealed class DatabaseManualOrderPaymentSelectionReader(MarketDbContext db)
    : IManualOrderPaymentSelectionReader
{
    public Task<bool> HasManualPaymentAsync(Guid orderId, CancellationToken cancellationToken)
    {
        if (orderId == Guid.Empty)
        {
            return Task.FromResult(false);
        }

        return db.Set<ManualOrderPayment>()
            .AnyAsync(payment => payment.OrderId == OrderId.From(orderId), cancellationToken);
    }
}
