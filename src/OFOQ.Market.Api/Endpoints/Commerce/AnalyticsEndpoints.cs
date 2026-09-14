using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Commerce.Analytics;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Analytics;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/analytics")
                .WithTags(
                    "Back Office Analytics")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        group.MapGet(
            "/summary",
            GetSummaryAsync);

        return endpoints;
    }

    private static async Task<IResult> GetSummaryAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int? topProducts,
        GetMerchantAnalyticsHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await handler.HandleAsync(
                    new GetMerchantAnalyticsQuery(
                        fromUtc,
                        toUtc,
                        topProducts ?? 5),
                    cancellationToken);

            return Results.Ok(
                new MerchantAnalyticsResponse(
                    result.FromUtc,
                    result.ToUtc,
                    result.TotalOrders,
                    result.CancelledOrders,
                    result.Sales
                        .Select(
                            item =>
                                new MerchantAnalyticsCurrencyResponse(
                                    item.Currency,
                                    item.PaidOrders,
                                    item.CapturedSales,
                                    item.AverageOrderValue))
                        .ToArray(),
                    result.TopProducts
                        .Select(
                            item =>
                                new MerchantAnalyticsTopProductResponse(
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
        catch (ArgumentOutOfRangeException exception)
        {
            return ValidationError(
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                exception.Message);
        }
    }

    private static IResult ValidationError(
        string message)
    {
        return Results.BadRequest(
            new
            {
                code =
                    "analytics_query_invalid",

                message
            });
    }
}