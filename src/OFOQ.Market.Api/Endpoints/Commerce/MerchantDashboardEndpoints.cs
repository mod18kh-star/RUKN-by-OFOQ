using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Commerce.Dashboard;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Dashboard;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class MerchantDashboardEndpoints
{
    public static IEndpointRouteBuilder MapMerchantDashboardEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/dashboard")
                .WithTags(
                    "Back Office Dashboard")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        group.MapGet(
            "/summary",
            GetSummaryAsync);

        return endpoints;
    }

    private static async Task<IResult> GetSummaryAsync(
        int? abandonedAfterMinutes,
        int? take,
        GetMerchantOperationsDashboardHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await handler.HandleAsync(
                    abandonedAfterMinutes
                    ?? GetMerchantOperationsDashboardHandler
                        .DefaultAbandonedAfterMinutes,
                    take ?? 10,
                    cancellationToken);

            return Results.Ok(
                new MerchantOperationsDashboardResponse(
                    new MerchantDashboardReadinessResponse(
                        result.Readiness.Percentage,
                        result.Readiness.State,
                        result.Readiness.StoreStatus),
                    result.OpenOrders,
                    result.PendingOrders,
                    result.PaidOrdersAwaitingConfirmation,
                    result.LowStockVariants,
                    result.AbandonedCarts,
                    result.AbandonedAfterMinutes,
                    result.RecentOrders
                        .Select(
                            item =>
                                new MerchantDashboardRecentOrderResponse(
                                    item.OrderId,
                                    item.Status,
                                    item.FulfillmentStatus,
                                    item.Currency,
                                    item.TotalAmount,
                                    item.CreatedAtUtc))
                        .ToArray(),
                    result.LowStock
                        .Select(
                            item =>
                                new MerchantDashboardLowStockResponse(
                                    item.ProductId,
                                    item.VariantId,
                                    item.ProductName,
                                    item.VariantName,
                                    item.Sku,
                                    item.Quantity,
                                    item.LowStockThreshold))
                        .ToArray(),
                    result.AbandonedCartItems
                        .Select(
                            item =>
                                new MerchantDashboardAbandonedCartResponse(
                                    item.CartId,
                                    item.CustomerUserId,
                                    item.Currency,
                                    item.TotalQuantity,
                                    item.TotalAmount,
                                    item.LastActivityAtUtc))
                        .ToArray(),
                    result.TopProducts
                        .Select(
                            item =>
                                new MerchantDashboardTopProductResponse(
                                    item.ProductId,
                                    item.ProductName,
                                    item.Currency,
                                    item.QuantitySold,
                                    item.CapturedSales))
                        .ToArray()));
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
                        "dashboard_query_invalid",
                    message =
                        exception.Message
                });
        }
    }
}
