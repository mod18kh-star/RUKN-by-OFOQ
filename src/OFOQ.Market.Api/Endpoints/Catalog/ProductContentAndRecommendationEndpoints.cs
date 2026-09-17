using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Catalog.ProductContentBlocks;
using OFOQ.Market.Application.Catalog.ProductRelations;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Catalog;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Catalog;

public static class ProductContentAndRecommendationEndpoints
{
    public static IEndpointRouteBuilder MapProductContentAndRecommendationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var products =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/products")
                .WithTags(
                    "Back Office Product Content & Recommendations")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        products.MapGet(
            "/{productId:guid}/content-blocks",
            GetContentBlocksAsync);

        products.MapPut(
            "/{productId:guid}/content-blocks",
            SetContentBlocksAsync);

        products.MapGet(
            "/{productId:guid}/recommendations",
            GetRelationsAsync);

        products.MapPut(
            "/{productId:guid}/recommendations/{relationType}",
            SetRelationsAsync);

        var settings =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/recommendations")
                .WithTags(
                    "Back Office Product Recommendations")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        settings.MapGet(
            "/settings",
            GetRecommendationSettingsAsync);

        settings.MapPut(
            "/settings",
            UpdateRecommendationSettingsAsync);

        return endpoints;
    }

    private static async Task<IResult> GetContentBlocksAsync(
        Guid productId,
        GetProductContentBlocksHandler handler,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
            return InvalidProductId();

        try
        {
            var result =
                await handler.HandleAsync(
                    ProductId.From(productId),
                    cancellationToken);

            return result is null
                ? ProductNotFound()
                : Results.Ok(
                    new ProductContentBlocksResponse(
                        result.ProductId,
                        result.Blocks
                            .Select(block =>
                                new ProductContentBlockResponse(
                                    block.BlockId,
                                    block.Type,
                                    block.Title,
                                    block.Body,
                                    block.MediaUrl,
                                    block.SortOrder,
                                    block.IsVisible))
                            .ToArray()));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> SetContentBlocksAsync(
        Guid productId,
        SetProductContentBlocksRequest request,
        SetProductContentBlocksHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
            return InvalidProductId();

        var actor = GetActor(httpContext);

        if (!actor.HasValue)
            return Results.Unauthorized();

        if (request.Blocks is null)
            return ValidationError("Product content blocks are required.");

        try
        {
            var inputs =
                request.Blocks
                    .Select(block =>
                    {
                        if (!Enum.TryParse<ProductContentBlockType>(
                                block.Type,
                                true,
                                out var type) ||
                            !Enum.IsDefined(type))
                        {
                            throw new ArgumentException(
                                $"Unsupported product content block type '{block.Type}'.");
                        }

                        return new ProductContentBlockInput(
                            type,
                            block.Title,
                            block.Body,
                            block.MediaUrl,
                            block.IsVisible);
                    })
                    .ToArray();

            var result =
                await handler.HandleAsync(
                    new SetProductContentBlocksCommand(
                        ProductId.From(productId),
                        inputs,
                        actor.Value),
                    cancellationToken);

            return result is null
                ? ProductNotFound()
                : Results.Ok(
                    new ProductContentBlocksResponse(
                        result.ProductId,
                        result.Blocks
                            .Select(block =>
                                new ProductContentBlockResponse(
                                    block.BlockId,
                                    block.Type,
                                    block.Title,
                                    block.Body,
                                    block.MediaUrl,
                                    block.SortOrder,
                                    block.IsVisible))
                            .ToArray()));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return ValidationError(exception.Message);
        }
    }

    private static async Task<IResult> GetRelationsAsync(
        Guid productId,
        GetProductRelationsHandler handler,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
            return InvalidProductId();

        try
        {
            var result =
                await handler.HandleAsync(
                    ProductId.From(productId),
                    cancellationToken);

            return result is null
                ? ProductNotFound()
                : Results.Ok(
                    Map(result));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> SetRelationsAsync(
        Guid productId,
        string relationType,
        SetProductRelationsRequest request,
        SetProductRelationsHandler handler,
        GetProductRelationsHandler getHandler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
            return InvalidProductId();

        var actor = GetActor(httpContext);

        if (!actor.HasValue)
            return Results.Unauthorized();

        if (!Enum.TryParse<ProductRelationType>(
                relationType,
                true,
                out var type) ||
            !Enum.IsDefined(type))
        {
            return ValidationError(
                $"Unsupported product relation type '{relationType}'.");
        }

        if (request.Relations is null)
            return ValidationError("Product relations are required.");

        try
        {
            var success =
                await handler.HandleAsync(
                    new SetProductRelationsCommand(
                        ProductId.From(productId),
                        type,
                        request.Relations
                            .Select(item =>
                            {
                                if (item.TargetProductId == Guid.Empty)
                                    throw new ArgumentException("Target product ID cannot be empty.");

                                return new ProductRelationInput(
                                    ProductId.From(item.TargetProductId),
                                    item.IsVisible);
                            })
                            .ToArray(),
                        actor.Value),
                    cancellationToken);

            if (!success)
                return ProductNotFound();

            var result =
                await getHandler.HandleAsync(
                    ProductId.From(productId),
                    cancellationToken);

            return result is null
                ? ProductNotFound()
                : Results.Ok(Map(result));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException exception)
        {
            return ValidationError(exception.Message);
        }
    }

    private static async Task<IResult> GetRecommendationSettingsAsync(
        ProductRecommendationSettingsHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await handler.GetAsync(
                    cancellationToken);

            return Results.Ok(
                new ProductRecommendationSettingsResponse(
                    result.IsEnabled,
                    result.AutomaticSuggestionsEnabled));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> UpdateRecommendationSettingsAsync(
        UpdateProductRecommendationSettingsRequest request,
        ProductRecommendationSettingsHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor = GetActor(httpContext);

        if (!actor.HasValue)
            return Results.Unauthorized();

        try
        {
            var result =
                await handler.UpdateAsync(
                    new UpdateProductRecommendationSettingsCommand(
                        request.IsEnabled,
                        request.AutomaticSuggestionsEnabled,
                        actor.Value),
                    cancellationToken);

            return Results.Ok(
                new ProductRecommendationSettingsResponse(
                    result.IsEnabled,
                    result.AutomaticSuggestionsEnabled));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static ProductRelationsResponse Map(
        ProductRelationsResult result)
    {
        return new ProductRelationsResponse(
            result.ProductId,
            result.RecommendationsEnabled,
            result.AutomaticSuggestionsEnabled,
            result.Relations
                .Select(relation =>
                    new ProductRelationResponse(
                        relation.RelationId,
                        relation.Type,
                        relation.TargetProductId,
                        relation.TargetProductName,
                        relation.TargetProductSlug,
                        relation.SortOrder,
                        relation.IsVisible))
                .ToArray());
    }

    private static UserId? GetActor(
        HttpContext httpContext)
    {
        var subject =
            httpContext.User
                .FindFirst(
                    JwtRegisteredClaimNames.Sub)?
                .Value;

        return Guid.TryParse(
                subject,
                out var actorGuid) &&
            actorGuid != Guid.Empty
                ? UserId.From(actorGuid)
                : null;
    }

    private static IResult InvalidProductId() =>
        Results.BadRequest(
            new
            {
                code = "invalid_product_id",
                message = "Product ID must be a valid non-empty GUID."
            });

    private static IResult ProductNotFound() =>
        Results.NotFound(
            new
            {
                code = "product_not_found",
                message = "Product was not found."
            });

    private static IResult ValidationError(
        string message) =>
        Results.BadRequest(
            new
            {
                code = "validation_error",
                message
            });
}
