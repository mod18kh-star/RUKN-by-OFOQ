using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Catalog.Products.CreateProduct;
using OFOQ.Market.Application.Catalog.Products.Variants;
using OFOQ.Market.Application.Catalog.Products.Variants.CreateVariant;
using OFOQ.Market.Application.Catalog.Products.Variants.GetVariants;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Catalog;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Catalog;

public static class ProductVariantEndpoints
{
    public static IEndpointRouteBuilder MapProductVariantEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/products/{productId:guid}/variants")
                .WithTags(
                    "Back Office Product Variants")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        group.MapPost(
            "/",
            CreateVariantAsync);

        group.MapGet(
            "/",
            GetVariantsAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateVariantAsync(
        Guid productId,
        CreateStructuredProductVariantRequest request,
        CreateProductVariantHandler handler,
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

        if (productId == Guid.Empty)
        {
            return InvalidProductId();
        }

        if (request.OptionValueIds is null)
        {
            return ValidationError(
                "Option value IDs are required.");
        }

        try
        {
            var valueIds =
                request.OptionValueIds
                    .Select(
                        valueId =>
                            ProductOptionValueId.From(
                                valueId))
                    .ToArray();

            var result =
                await handler.HandleAsync(
                    new CreateProductVariantCommand(
                        ProductId.From(
                            productId),
                        request.Name,
                        request.Sku,
                        request.PriceOverride,
                        request.TrackInventory,
                        request.Quantity,
                        request.LowStockThreshold,
                        request.ContinueSellingWhenOutOfStock,
                        valueIds,
                        actor.Value),
                    cancellationToken);

            return Results.Created(
                $"{httpContext.Request.Path}/{result.VariantId.Value}",
                Map(result));
        }
        catch (ProductVariantProductNotFoundException)
        {
            return ProductNotFound();
        }
        catch (ProductSkuAlreadyExistsException exception)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "product_sku_already_exists",

                    message =
                        exception.Message
                });
        }
        catch (ProductVariantCombinationAlreadyExistsException exception)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "product_variant_combination_already_exists",

                    message =
                        exception.Message
                });
        }
        catch (ProductVariantOptionValueNotFoundException)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "product_variant_option_value_not_found",

                    message =
                        "One of the selected option values was not found."
                });
        }
        catch (ProductVariantInvalidOptionSelectionException exception)
        {
            return ValidationError(
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

    private static async Task<IResult> GetVariantsAsync(
        Guid productId,
        GetProductVariantsHandler handler,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return InvalidProductId();
        }

        try
        {
            var results =
                await handler.HandleAsync(
                    ProductId.From(
                        productId),
                    cancellationToken);

            return Results.Ok(
                results
                    .Select(
                        Map)
                    .ToArray());
        }
        catch (ProductVariantProductNotFoundException)
        {
            return ProductNotFound();
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
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
                out var userId) ||
            userId == Guid.Empty)
        {
            return null;
        }

        return UserId.From(
            userId);
    }

    private static StructuredProductVariantResponse Map(
        StructuredProductVariantResult result)
    {
        return new StructuredProductVariantResponse(
            result.VariantId.Value,
            result.Name,
            result.Sku,
            result.IsDefault,
            result.PriceOverride,
            result.PriceOverrideCurrency,
            result.TrackInventory,
            result.Quantity,
            result.LowStockThreshold,
            result.ContinueSellingWhenOutOfStock,
            result.IsAvailableForSale,
            result.IsEnabled,
            result.Selections
                .Select(
                    selection =>
                        new ProductVariantSelectionResponse(
                            selection.OptionId.Value,
                            selection.OptionName,
                            selection.ValueId.Value,
                            selection.Value))
                .ToArray());
    }

    private static IResult InvalidProductId()
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

    private static IResult ProductNotFound()
    {
        return Results.NotFound(
            new
            {
                code =
                    "product_not_found",

                message =
                    "Product was not found."
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