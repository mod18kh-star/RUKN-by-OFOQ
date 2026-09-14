using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Catalog.ProductAttributes;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Catalog;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Catalog;

public static class ProductAttributeEndpoints
{
    public static IEndpointRouteBuilder MapProductAttributeEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/products")
                .WithTags(
                    "Back Office Product Attributes")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        group.MapGet(
            "/{productId:guid}/attributes",
            GetAsync);

        group.MapPut(
            "/{productId:guid}/attributes",
            SetAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        Guid productId,
        GetProductAttributesHandler handler,
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

            if (result is null)
            {
                return ProductNotFound();
            }

            return Results.Ok(
                Map(
                    result));
        }
        catch (ProductAttributesVerticalNotConfiguredException exception)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "commerce_vertical_not_configured",

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

    private static async Task<IResult> SetAsync(
        Guid productId,
        SetProductAttributesRequest request,
        SetProductAttributesHandler handler,
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

        if (request.Values is null)
        {
            return ValidationError(
                "Product attribute values are required.");
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new SetProductAttributesCommand(
                        ProductId.From(
                            productId),
                        request.Values
                            .Select(
                                item =>
                                    new ProductAttributeInput(
                                        item.Key,
                                        item.Value))
                            .ToArray(),
                        actor.Value),
                    cancellationToken);

            if (result is null)
            {
                return ProductNotFound();
            }

            return Results.Ok(
                Map(
                    result));
        }
        catch (ProductAttributesVerticalNotConfiguredException exception)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "commerce_vertical_not_configured",

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

    private static ProductAttributesResponse Map(
        ProductAttributesResult result)
    {
        return new ProductAttributesResponse(
            result.ProductId,
            result.Vertical,
            result.VerticalCode,
            result.Attributes
                .Select(
                    attribute =>
                        new ProductAttributeFieldResponse(
                            attribute.Key,
                            attribute.Label,
                            attribute.ValueType,
                            attribute.AllowedValues,
                            attribute.Value))
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
                out var userGuid) ||
            userGuid ==
            Guid.Empty)
        {
            return null;
        }

        return UserId.From(
            userGuid);
    }

    private static IResult InvalidProductId()
    {
        return ValidationError(
            "A valid product ID is required.");
    }

    private static IResult ProductNotFound()
    {
        return Results.NotFound(
            new
            {
                code =
                    "product_not_found",

                message =
                    "The product was not found."
            });
    }

    private static IResult ValidationError(
        string message)
    {
        return Results.BadRequest(
            new
            {
                code =
                    "product_attributes_invalid",

                message
            });
    }
}