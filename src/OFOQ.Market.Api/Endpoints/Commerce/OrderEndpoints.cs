using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Commerce.Orders.Cancel;
using OFOQ.Market.Application.Commerce.Orders.Common;
using OFOQ.Market.Application.Commerce.Orders.Queries;
using OFOQ.Market.Application.Commerce.Orders.State;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/orders")
                .WithTags(
                    "Back Office Orders")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        group.MapGet(
            "/",
            GetOrdersAsync);

        group.MapGet(
            "/{orderId:guid}",
            GetOrderByIdAsync);

        group.MapPost(
            "/{orderId:guid}/confirm",
            ConfirmAsync);

        group.MapPost(
            "/{orderId:guid}/processing",
            StartProcessingAsync);

        group.MapPost(
            "/{orderId:guid}/ready-to-ship",
            ReadyToShipAsync);

        group.MapPost(
            "/{orderId:guid}/ship",
            ShipAsync);

        group.MapPost(
            "/{orderId:guid}/in-transit",
            MarkInTransitAsync);

        group.MapPost(
            "/{orderId:guid}/deliver",
            DeliverAsync);

        group.MapPost(
            "/{orderId:guid}/cancel",
            CancelAsync);

        return endpoints;
    }

    private static async Task<IResult> GetOrdersAsync(
        string? status,
        string? fulfillmentStatus,
        int? take,
        GetMerchantOrdersHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryParseOrderStatus(
                status,
                out var parsedStatus))
        {
            return ValidationError(
                "order_status_invalid",
                "A supported order status is required.");
        }

        if (!TryParseFulfillmentStatus(
                fulfillmentStatus,
                out var parsedFulfillmentStatus))
        {
            return ValidationError(
                "order_fulfillment_status_invalid",
                "A supported order fulfillment status is required.");
        }

        var resolvedTake =
            take ?? 50;

        try
        {
            var results =
                await handler.HandleAsync(
                    new GetMerchantOrdersQuery(
                        parsedStatus,
                        parsedFulfillmentStatus,
                        resolvedTake),
                    cancellationToken);

            return Results.Ok(
                results
                    .Select(
                        MapSummary)
                    .ToArray());
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return ValidationError(
                "order_query_invalid",
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                "order_query_invalid",
                exception.Message);
        }
    }

    private static async Task<IResult> GetOrderByIdAsync(
        Guid orderId,
        GetMerchantOrderByIdHandler handler,
        CancellationToken cancellationToken)
    {
        if (orderId == Guid.Empty)
        {
            return InvalidOrderId();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new GetMerchantOrderByIdQuery(
                        OrderId.From(
                            orderId)),
                    cancellationToken);

            if (result is null)
            {
                return OrderNotFound();
            }

            return Results.Ok(
                MapDetail(
                    result));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                "order_query_invalid",
                exception.Message);
        }
    }

    private static Task<IResult> ConfirmAsync(
        Guid orderId,
        ChangeOrderLifecycleHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ChangeLifecycleAsync(
            orderId,
            OrderLifecycleAction.Confirm,
            null,
            null,
            handler,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> StartProcessingAsync(
        Guid orderId,
        ChangeOrderLifecycleHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ChangeLifecycleAsync(
            orderId,
            OrderLifecycleAction.StartProcessing,
            null,
            null,
            handler,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> ReadyToShipAsync(
        Guid orderId,
        ChangeOrderLifecycleHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ChangeLifecycleAsync(
            orderId,
            OrderLifecycleAction.ReadyToShip,
            null,
            null,
            handler,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> MarkInTransitAsync(
        Guid orderId,
        ChangeOrderLifecycleHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ChangeLifecycleAsync(
            orderId,
            OrderLifecycleAction.MarkInTransit,
            null,
            null,
            handler,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> DeliverAsync(
        Guid orderId,
        ChangeOrderLifecycleHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ChangeLifecycleAsync(
            orderId,
            OrderLifecycleAction.Deliver,
            null,
            null,
            handler,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> ShipAsync(
        Guid orderId,
        ShipOrderRequest request,
        ChangeOrderLifecycleHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ChangeLifecycleAsync(
            orderId,
            OrderLifecycleAction.Ship,
            request.ShippingCarrier,
            request.TrackingNumber,
            handler,
            httpContext,
            cancellationToken);
    }

    private static async Task<IResult> ChangeLifecycleAsync(
        Guid orderId,
        OrderLifecycleAction action,
        string? shippingCarrier,
        string? trackingNumber,
        ChangeOrderLifecycleHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor =
            GetActorUserId(
                httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (orderId == Guid.Empty)
        {
            return InvalidOrderId();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new ChangeOrderLifecycleCommand(
                        OrderId.From(
                            orderId),
                        action,
                        actor.Value,
                        shippingCarrier,
                        trackingNumber),
                    cancellationToken);

            if (result is null)
            {
                return OrderNotFound();
            }

            return Results.Ok(
                MapLifecycle(
                    result));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(
                "order_state_conflict",
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                "order_validation_error",
                exception.Message);
        }
    }

    private static async Task<IResult> CancelAsync(
        Guid orderId,
        CancelOrderRequest request,
        CancelOrderHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor =
            GetActorUserId(
                httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (orderId == Guid.Empty)
        {
            return InvalidOrderId();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new CancelOrderCommand(
                        OrderId.From(
                            orderId),
                        request.Reason,
                        actor.Value),
                    cancellationToken);

            if (result is null)
            {
                return OrderNotFound();
            }

            return Results.Ok(
                MapCancellation(
                    result));
        }
        catch (OrderCancellationInventoryStateException exception)
        {
            return Conflict(
                "order_inventory_state_conflict",
                exception.Message);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(
                "order_state_conflict",
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                "order_validation_error",
                exception.Message);
        }
    }

    private static bool TryParseOrderStatus(
        string? value,
        out OrderStatus? status)
    {
        status =
            null;

        if (string.IsNullOrWhiteSpace(
                value))
        {
            return true;
        }

        if (!Enum.TryParse<OrderStatus>(
                value.Trim(),
                ignoreCase: true,
                out var parsed) ||
            !Enum.IsDefined(
                parsed))
        {
            return false;
        }

        status =
            parsed;

        return true;
    }

    private static bool TryParseFulfillmentStatus(
        string? value,
        out OrderFulfillmentStatus? status)
    {
        status =
            null;

        if (string.IsNullOrWhiteSpace(
                value))
        {
            return true;
        }

        if (!Enum.TryParse<OrderFulfillmentStatus>(
                value.Trim(),
                ignoreCase: true,
                out var parsed) ||
            !Enum.IsDefined(
                parsed))
        {
            return false;
        }

        status =
            parsed;

        return true;
    }

    private static UserId? GetActorUserId(
        HttpContext httpContext)
    {
        var subject =
            httpContext.User
                .FindFirst(
                    JwtRegisteredClaimNames.Sub)?
                .Value;

        if (!Guid.TryParse(
                subject,
                out var userGuid) ||
            userGuid ==
            Guid.Empty)
        {
            return null;
        }

        return UserId.From(
            userGuid);
    }

    private static MerchantOrderSummaryResponse MapSummary(
        MerchantOrderSummaryResult result)
    {
        return new MerchantOrderSummaryResponse(
            result.OrderId,
            result.CustomerUserId,
            result.CustomerEmail,
            result.OrderStatus,
            result.PaymentStatus,
            result.FulfillmentStatus,
            result.Currency,
            result.TotalQuantity,
            result.TotalAmount,
            result.ShippingCarrier,
            result.TrackingNumber,
            result.CreatedAtUtc,
            result.UpdatedAtUtc);
    }

    private static MerchantOrderDetailResponse MapDetail(
        MerchantOrderDetailResult result)
    {
        return new MerchantOrderDetailResponse(
            result.OrderId,
            result.CustomerUserId,
            result.CustomerEmail,
            result.OrderStatus,
            result.PaymentStatus,
            result.FulfillmentStatus,
            result.Currency,
            result.TotalQuantity,
            result.TotalAmount,
            result.ShippingCarrier,
            result.TrackingNumber,
            result.CancellationReason,
            result.CreatedAtUtc,
            result.UpdatedAtUtc,
            result.ShippedAtUtc,
            result.DeliveredAtUtc,
            result.CancelledAtUtc,
            result.Items
                .Select(
                    item =>
                        new MerchantOrderItemResponse(
                            item.OrderItemId,
                            item.ProductId,
                            item.ProductVariantId,
                            item.ProductName,
                            item.VariantName,
                            item.Sku,
                            item.UnitPrice,
                            item.Currency,
                            item.Quantity,
                            item.LineTotal))
                .ToArray(),
            result.Timeline
                .Select(
                    entry =>
                        new MerchantOrderTimelineResponse(
                            entry.Type,
                            entry.OrderStatus,
                            entry.FulfillmentStatus,
                            entry.Note,
                            entry.CreatedAtUtc))
                .ToArray());
    }

    private static OrderLifecycleResponse MapLifecycle(
        OrderLifecycleResult result)
    {
        return new OrderLifecycleResponse(
            result.OrderId,
            result.OrderStatus,
            result.FulfillmentStatus,
            result.ShippingCarrier,
            result.TrackingNumber,
            result.UpdatedAtUtc,
            result.IsIdempotentReplay);
    }

    private static CancelOrderResponse MapCancellation(
        CancelOrderResult result)
    {
        return new CancelOrderResponse(
            result.OrderId.Value,
            result.Status.ToString(),
            result.FulfillmentStatus.ToString(),
            result.CancellationReason,
            result.CancelledAtUtc,
            result.IsIdempotentReplay,
            result.Inventory
                .Select(
                    item =>
                        new OrderInventoryRestockResponse(
                            item.ProductVariantId.Value,
                            item.RestoredQuantity,
                            item.QuantityAfter))
                .ToArray());
    }

    private static IResult InvalidOrderId()
    {
        return ValidationError(
            "order_id_invalid",
            "A valid order ID is required.");
    }

    private static IResult OrderNotFound()
    {
        return Results.NotFound(
            new
            {
                code =
                    "order_not_found",

                message =
                    "The order was not found."
            });
    }

    private static IResult ValidationError(
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