using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Catalog.Categories;
using OFOQ.Market.Application.Catalog.Categories.CreateCategory;
using OFOQ.Market.Application.Catalog.Categories.GetCategories;
using OFOQ.Market.Application.Catalog.Categories.GetCategoryById;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Catalog;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Catalog;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/categories")
                .WithTags(
                    "Back Office Categories")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        group.MapPost(
            "/",
            CreateCategoryAsync);

        group.MapGet(
            "/",
            GetCategoriesAsync);

        group.MapGet(
            "/{categoryId:guid}",
            GetCategoryByIdAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateCategoryAsync(
        CreateCategoryRequest request,
        CreateCategoryHandler handler,
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

        CategoryId? parentCategoryId =
            null;

        if (request.ParentCategoryId.HasValue)
        {
            if (request.ParentCategoryId.Value ==
                Guid.Empty)
            {
                return Results.BadRequest(
                    new
                    {
                        code =
                            "invalid_parent_category_id",

                        message =
                            "Parent category ID must be a valid non-empty GUID."
                    });
            }

            parentCategoryId =
                CategoryId.From(
                    request.ParentCategoryId.Value);
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new CreateCategoryCommand(
                        request.Name,
                        request.Slug,
                        parentCategoryId,
                        request.SortOrder,
                        UserId.From(
                            actorGuid)),
                    cancellationToken);

            var response =
                Map(
                    result);

            return Results.Created(
                $"/api/tenants/{httpContext.Request.RouteValues["tenantId"]}/backoffice/categories/{result.CategoryId.Value}",
                response);
        }
        catch (CategorySlugAlreadyExistsException exception)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "category_slug_already_exists",

                    message =
                        exception.Message
                });
        }
        catch (CategoryParentNotFoundException)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "category_parent_not_found",

                    message =
                        "The requested parent category was not found."
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

    private static async Task<IResult> GetCategoriesAsync(
        GetCategoriesHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var results =
                await handler.HandleAsync(
                    cancellationToken);

            var response =
                results
                    .Select(
                        Map)
                    .ToArray();

            return Results.Ok(
                response);
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> GetCategoryByIdAsync(
        Guid categoryId,
        GetCategoryByIdHandler handler,
        CancellationToken cancellationToken)
    {
        if (categoryId ==
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

        try
        {
            var result =
                await handler.HandleAsync(
                    CategoryId.From(
                        categoryId),
                    cancellationToken);

            if (result is null)
            {
                return Results.NotFound(
                    new
                    {
                        code =
                            "category_not_found",

                        message =
                            "Category was not found."
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

    private static CategoryResponse Map(
        CategoryResult result)
    {
        return new CategoryResponse(
            result.CategoryId.Value,
            result.Name,
            result.Slug,
            result.ParentCategoryId?.Value,
            result.SortOrder,
            result.IsVisible,
            result.CreatedAtUtc);
    }
}