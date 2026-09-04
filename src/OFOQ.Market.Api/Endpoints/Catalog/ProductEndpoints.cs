using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Catalog.Products;
using OFOQ.Market.Application.Catalog.Products.CreateProduct;
using OFOQ.Market.Application.Catalog.Products.GetProductById;
using OFOQ.Market.Application.Catalog.Products.GetProducts;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Catalog;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Catalog;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/products")
                .WithTags(
                    "Back Office Products")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        group.MapPost(
            "/",
            CreateProductAsync);

        group.MapGet(
            "/",
            GetProductsAsync);

        group.MapGet(
            "/{productId:guid}",
            GetProductByIdAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateProductAsync(
        CreateProductRequest request,
        CreateProductHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var subject =
            httpContext.User
                .FindFirst(
                    JwtRegisteredClaimNames.Sub)?
                .Value;

        if (!Guid.TryParse(
                subject,
                out var actorGuid) ||
            actorGuid == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        CategoryId? categoryId =
            null;

        if (request.CategoryId.HasValue)
        {
            if (request.CategoryId.Value ==
                Guid.Empty)
            {
                return Results.BadRequest(
                    new
                    {
                        code =
                            "invalid_category_id",

                        message =
                            "Category ID must be a valid non-empty GUID."
                    });
            }

            categoryId =
                CategoryId.From(
                    request.CategoryId.Value);
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new CreateProductCommand(
                        request.Name,
                        request.Slug,
                        request.Description,
                        categoryId,
                        request.Price,
                        request.CompareAtPrice,
                        request.Currency,
                        request.Sku,
                        request.TrackInventory,
                        request.Quantity,
                        request.LowStockThreshold,
                        request.ContinueSellingWhenOutOfStock,
                        UserId.From(
                            actorGuid)),
                    cancellationToken);

            return Results.Created(
                $"/api/tenants/{httpContext.Request.RouteValues["tenantId"]}/backoffice/products/{result.ProductId.Value}",
                Map(
                    result));
        }
        catch (ProductSlugAlreadyExistsException exception)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "product_slug_already_exists",

                    message =
                        exception.Message
                });
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
        catch (ProductCategoryNotFoundException)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "product_category_not_found",

                    message =
                        "The requested product category was not found."
                });
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
                        "validation_error",

                    message =
                        exception.Message
                });
        }
    }

    private static async Task<IResult> GetProductsAsync(
        GetProductsHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var results =
                await handler.HandleAsync(
                    cancellationToken);

            return Results.Ok(
                results
                    .Select(
                        Map)
                    .ToArray());
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> GetProductByIdAsync(
        Guid productId,
        GetProductByIdHandler handler,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
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

        try
        {
            var result =
                await handler.HandleAsync(
                    ProductId.From(
                        productId),
                    cancellationToken);

            if (result is null)
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

            return Results.Ok(
                Map(
                    result));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static ProductResponse Map(
        ProductResult result)
    {
        return new ProductResponse(
            result.ProductId.Value,
            result.Name,
            result.Slug,
            result.Description,
            result.CategoryId?.Value,
            result.Price,
            result.Currency,
            result.CompareAtPrice,
            result.Status.ToString(),
            result.IsVisible,
            result.CreatedAtUtc,
            result.Variants
                .Select(
                    variant =>
                        new ProductVariantResponse(
                            variant.VariantId.Value,
                            variant.Name,
                            variant.Sku,
                            variant.IsDefault,
                            variant.PriceOverride,
                            variant.PriceOverrideCurrency,
                            variant.TrackInventory,
                            variant.Quantity,
                            variant.LowStockThreshold,
                            variant.ContinueSellingWhenOutOfStock,
                            variant.IsAvailableForSale,
                            variant.IsEnabled))
                .ToArray());
    }
}