using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Catalog.Products;
using OFOQ.Market.Application.Catalog.Products.ChangeState;
using OFOQ.Market.Application.Catalog.Products.CreateProduct;
using OFOQ.Market.Application.Catalog.Products.GetProductById;
using OFOQ.Market.Application.Catalog.Products.GetProducts;
using OFOQ.Market.Application.Catalog.Products.Inventory;
using OFOQ.Market.Application.Catalog.Products.UpdateProduct;
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

        group.MapPut(
            "/{productId:guid}",
            UpdateProductAsync);

        group.MapPut(
            "/{productId:guid}/inventory",
            UpdateInventoryAsync);

        group.MapPost(
            "/{productId:guid}/publish",
            PublishProductAsync);

        group.MapPost(
            "/{productId:guid}/draft",
            MoveProductToDraftAsync);

        group.MapPost(
            "/{productId:guid}/archive",
            ArchiveProductAsync);

        group.MapPost(
            "/{productId:guid}/show",
            ShowProductAsync);

        group.MapPost(
            "/{productId:guid}/hide",
            HideProductAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateProductAsync(
        CreateProductRequest request,
        CreateProductHandler handler,
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

        CategoryId? categoryId;

        try
        {
            categoryId =
                ParseCategoryId(
                    request.CategoryId);
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                exception.Message);
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
                        actor.Value),
                    cancellationToken);

            return Results.Created(
                $"/api/tenants/{httpContext.Request.RouteValues["tenantId"]}/backoffice/products/{result.ProductId.Value}",
                Map(result));
        }
        catch (ProductSlugAlreadyExistsException exception)
        {
            return Conflict(
                "product_slug_already_exists",
                exception.Message);
        }
        catch (ProductSkuAlreadyExistsException exception)
        {
            return Conflict(
                "product_sku_already_exists",
                exception.Message);
        }
        catch (ProductCategoryNotFoundException)
        {
            return ProductCategoryNotFound();
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

    private static async Task<IResult> UpdateProductAsync(
        Guid productId,
        UpdateProductRequest request,
        UpdateProductHandler handler,
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

        CategoryId? categoryId;

        try
        {
            categoryId =
                ParseCategoryId(
                    request.CategoryId);
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                exception.Message);
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new UpdateProductCommand(
                        ProductId.From(productId),
                        request.Name,
                        request.Slug,
                        request.Description,
                        categoryId,
                        request.Price,
                        request.CompareAtPrice,
                        request.Currency,
                        request.Sku,
                        actor.Value),
                    cancellationToken);

            if (result is null)
            {
                return ProductNotFound();
            }

            return Results.Ok(
                Map(result));
        }
        catch (ProductSlugAlreadyExistsException exception)
        {
            return Conflict(
                "product_slug_already_exists",
                exception.Message);
        }
        catch (ProductSkuAlreadyExistsException exception)
        {
            return Conflict(
                "product_sku_already_exists",
                exception.Message);
        }
        catch (ProductCategoryNotFoundException)
        {
            return ProductCategoryNotFound();
        }
        catch (ProductDefaultVariantNotFoundException exception)
        {
            return Conflict(
                "product_default_variant_not_found",
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

    private static async Task<IResult> UpdateInventoryAsync(
        Guid productId,
        UpdateProductInventoryRequest request,
        UpdateProductInventoryHandler handler,
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

        try
        {
            var result =
                await handler.HandleAsync(
                    new UpdateProductInventoryCommand(
                        ProductId.From(productId),
                        request.TrackInventory,
                        request.Quantity,
                        request.LowStockThreshold,
                        request.ContinueSellingWhenOutOfStock,
                        actor.Value),
                    cancellationToken);

            if (result is null)
            {
                return ProductNotFound();
            }

            return Results.Ok(
                Map(result));
        }
        catch (ProductDefaultVariantNotFoundException exception)
        {
            return Conflict(
                "product_default_variant_not_found",
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

    private static Task<IResult> PublishProductAsync(
        Guid productId,
        ChangeProductStateHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            productId,
            ProductStateAction.Publish,
            handler,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> MoveProductToDraftAsync(
        Guid productId,
        ChangeProductStateHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            productId,
            ProductStateAction.MoveToDraft,
            handler,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> ArchiveProductAsync(
        Guid productId,
        ChangeProductStateHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            productId,
            ProductStateAction.Archive,
            handler,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> ShowProductAsync(
        Guid productId,
        ChangeProductStateHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            productId,
            ProductStateAction.Show,
            handler,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> HideProductAsync(
        Guid productId,
        ChangeProductStateHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            productId,
            ProductStateAction.Hide,
            handler,
            httpContext,
            cancellationToken);
    }

    private static async Task<IResult> ChangeStateAsync(
        Guid productId,
        ProductStateAction action,
        ChangeProductStateHandler handler,
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

        try
        {
            var result =
                await handler.HandleAsync(
                    ProductId.From(productId),
                    action,
                    actor.Value,
                    cancellationToken);

            if (result is null)
            {
                return ProductNotFound();
            }

            return Results.Ok(
                Map(result));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(
                "invalid_product_state",
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                exception.Message);
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
                    .Select(Map)
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
            return InvalidProductId();
        }

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
            actorGuid == Guid.Empty)
        {
            return null;
        }

        return UserId.From(
            actorGuid);
    }

    private static CategoryId? ParseCategoryId(
        Guid? categoryId)
    {
        if (!categoryId.HasValue)
        {
            return null;
        }

        if (categoryId.Value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Category ID must be a valid non-empty GUID.");
        }

        return CategoryId.From(
            categoryId.Value);
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

    private static IResult ProductCategoryNotFound()
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