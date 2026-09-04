using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Catalog.Products.Options;
using OFOQ.Market.Application.Catalog.Products.Options.CreateOption;
using OFOQ.Market.Application.Catalog.Products.Options.CreateValue;
using OFOQ.Market.Application.Catalog.Products.Options.GetOptions;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Catalog;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Catalog;

public static class ProductOptionEndpoints
{
    public static IEndpointRouteBuilder MapProductOptionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/products/{productId:guid}/options")
                .WithTags(
                    "Back Office Product Options")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        group.MapPost(
            "/",
            CreateOptionAsync);

        group.MapGet(
            "/",
            GetOptionsAsync);

        group.MapPost(
            "/{optionId:guid}/values",
            CreateValueAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateOptionAsync(
        Guid productId,
        CreateProductOptionRequest request,
        CreateProductOptionHandler handler,
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
                    new CreateProductOptionCommand(
                        ProductId.From(
                            productId),
                        request.Name,
                        request.SortOrder,
                        actor.Value),
                    cancellationToken);

            return Results.Created(
                $"{httpContext.Request.Path}/{result.OptionId.Value}",
                Map(result));
        }
        catch (ProductOptionProductNotFoundException)
        {
            return ProductNotFound();
        }
        catch (ProductOptionNameAlreadyExistsException exception)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "product_option_name_already_exists",

                    message =
                        exception.Message
                });
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

    private static async Task<IResult> CreateValueAsync(
        Guid productId,
        Guid optionId,
        CreateProductOptionValueRequest request,
        CreateProductOptionValueHandler handler,
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

        if (optionId == Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "invalid_product_option_id",

                    message =
                        "Product option ID must be a valid non-empty GUID."
                });
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new CreateProductOptionValueCommand(
                        ProductId.From(
                            productId),
                        ProductOptionId.From(
                            optionId),
                        request.Value,
                        request.SortOrder,
                        actor.Value),
                    cancellationToken);

            return Results.Created(
                $"{httpContext.Request.Path}/{result.ValueId.Value}",
                new ProductOptionValueResponse(
                    result.ValueId.Value,
                    result.Value,
                    result.SortOrder));
        }
        catch (ProductOptionProductNotFoundException)
        {
            return ProductNotFound();
        }
        catch (ProductOptionNotFoundException)
        {
            return Results.NotFound(
                new
                {
                    code =
                        "product_option_not_found",

                    message =
                        "Product option was not found."
                });
        }
        catch (ProductOptionValueAlreadyExistsException exception)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "product_option_value_already_exists",

                    message =
                        exception.Message
                });
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

    private static async Task<IResult> GetOptionsAsync(
        Guid productId,
        GetProductOptionsHandler handler,
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
        catch (ProductOptionProductNotFoundException)
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

    private static ProductOptionResponse Map(
        ProductOptionResult result)
    {
        return new ProductOptionResponse(
            result.OptionId.Value,
            result.Name,
            result.SortOrder,
            result.Values
                .Select(
                    value =>
                        new ProductOptionValueResponse(
                            value.ValueId.Value,
                            value.Value,
                            value.SortOrder))
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