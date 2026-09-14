using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Catalog.ProductImages;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Catalog;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Catalog;

public static class ProductImageEndpoints
{
    public static IEndpointRouteBuilder MapProductImageEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/products")
                .WithTags(
                    "Back Office Product Images")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        group.MapGet(
            "/{productId:guid}/images",
            GetAsync);

        group.MapPut(
            "/{productId:guid}/images",
            SetAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        Guid productId,
        GetProductImagesHandler handler,
        CancellationToken cancellationToken)
    {
        if (productId ==
            Guid.Empty)
        {
            return InvalidProductId();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    ProductId.From(
                        productId),
                    cancellationToken);

            return result is null
                ? ProductNotFound()
                : Results.Ok(
                    Map(
                        result));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> SetAsync(
        Guid productId,
        SetProductImagesRequest request,
        SetProductImagesHandler handler,
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

        if (productId ==
            Guid.Empty)
        {
            return InvalidProductId();
        }

        if (request.Images is null)
        {
            return ValidationError(
                "Product images are required.");
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new SetProductImagesCommand(
                        ProductId.From(
                            productId),
                        request.Images
                            .Select(
                                image =>
                                    new ProductImageInput(
                                        image.Url,
                                        image.AltText,
                                        image.IsPrimary))
                            .ToArray(),
                        actor.Value),
                    cancellationToken);

            return result is null
                ? ProductNotFound()
                : Results.Ok(
                    Map(
                        result));
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

    private static ProductImagesResponse Map(
        ProductImagesResult result)
    {
        return new ProductImagesResponse(
            result.ProductId,
            result.Images
                .Select(
                    image =>
                        new ProductImageResponse(
                            image.ImageId,
                            image.Url,
                            image.AltText,
                            image.SortOrder,
                            image.IsPrimary))
                .ToArray());
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
                out var actorGuid) ||
            actorGuid ==
            Guid.Empty)
        {
            return null;
        }

        return UserId.From(
            actorGuid);
    }

    private static IResult InvalidProductId() =>
        Results.BadRequest(
            new
            {
                code =
                    "invalid_product_id",

                message =
                    "Product ID must be a valid non-empty GUID."
            });

    private static IResult ProductNotFound() =>
        Results.NotFound(
            new
            {
                code =
                    "product_not_found",

                message =
                    "Product was not found."
            });

    private static IResult ValidationError(
        string message) =>
        Results.BadRequest(
            new
            {
                code =
                    "product_images_invalid",

                message
            });
}