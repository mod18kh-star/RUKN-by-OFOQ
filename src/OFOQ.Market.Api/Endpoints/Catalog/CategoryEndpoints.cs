using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Catalog.Categories;
using OFOQ.Market.Application.Catalog.Categories.CreateCategory;
using OFOQ.Market.Application.Catalog.Categories.GetCategories;
using OFOQ.Market.Application.Catalog.Categories.GetCategoryById;
using OFOQ.Market.Application.Catalog.Categories.MoveCategory;
using OFOQ.Market.Application.Catalog.Categories.UpdateCategory;
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

        group.MapPut(
            "/{categoryId:guid}",
            UpdateCategoryAsync);

        group.MapPut(
            "/{categoryId:guid}/placement",
            MoveCategoryAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateCategoryAsync(
        CreateCategoryRequest request,
        CreateCategoryHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(
                httpContext,
                out var actorUserId))
        {
            return Results.Unauthorized();
        }

        if (!TryMapParentCategoryId(
                request.ParentCategoryId,
                out var parentCategoryId,
                out var validationResult))
        {
            return validationResult!;
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new CreateCategoryCommand(
                        request.Name,
                        request.Slug,
                        parentCategoryId,
                        request.Position,
                        request.SortOrder,
                        actorUserId,
                        request.ImageUrl),
                    cancellationToken);

            return Results.Created(
                $"/api/tenants/{httpContext.Request.RouteValues["tenantId"]}/backoffice/categories/{result.CategoryId.Value}",
                Map(
                    result));
        }
        catch (Exception exception)
        {
            return MapException(
                exception);
        }
    }

    private static async Task<IResult> UpdateCategoryAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        UpdateCategoryHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (categoryId ==
            Guid.Empty)
        {
            return InvalidCategoryId();
        }

        if (!TryGetActorUserId(
                httpContext,
                out var actorUserId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new UpdateCategoryCommand(
                        CategoryId.From(
                            categoryId),
                        request.Name,
                        request.Slug,
                        request.IsVisible,
                        actorUserId,
                        request.ImageUrl),
                    cancellationToken);

            return Results.Ok(
                Map(
                    result));
        }
        catch (Exception exception)
        {
            return MapException(
                exception);
        }
    }

    private static async Task<IResult> MoveCategoryAsync(
        Guid categoryId,
        MoveCategoryRequest request,
        MoveCategoryHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (categoryId ==
            Guid.Empty)
        {
            return InvalidCategoryId();
        }

        if (!TryGetActorUserId(
                httpContext,
                out var actorUserId))
        {
            return Results.Unauthorized();
        }

        if (!TryMapParentCategoryId(
                request.ParentCategoryId,
                out var parentCategoryId,
                out var validationResult))
        {
            return validationResult!;
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new MoveCategoryCommand(
                        CategoryId.From(
                            categoryId),
                        parentCategoryId,
                        request.Position,
                        actorUserId),
                    cancellationToken);

            return Results.Ok(
                Map(
                    result));
        }
        catch (Exception exception)
        {
            return MapException(
                exception);
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

    private static async Task<IResult> GetCategoryByIdAsync(
        Guid categoryId,
        GetCategoryByIdHandler handler,
        CancellationToken cancellationToken)
    {
        if (categoryId ==
            Guid.Empty)
        {
            return InvalidCategoryId();
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

    private static bool TryGetActorUserId(
        HttpContext httpContext,
        out UserId actorUserId)
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
            actorUserId =
                default;

            return false;
        }

        actorUserId =
            UserId.From(
                actorGuid);

        return true;
    }

    private static bool TryMapParentCategoryId(
        Guid? parentCategoryGuid,
        out CategoryId? parentCategoryId,
        out IResult? validationResult)
    {
        parentCategoryId =
            null;

        validationResult =
            null;

        if (!parentCategoryGuid.HasValue)
        {
            return true;
        }

        if (parentCategoryGuid.Value ==
            Guid.Empty)
        {
            validationResult =
                Results.BadRequest(
                    new
                    {
                        code =
                            "invalid_parent_category_id",

                        message =
                            "Parent category ID must be a valid non-empty GUID."
                    });

            return false;
        }

        parentCategoryId =
            CategoryId.From(
                parentCategoryGuid.Value);

        return true;
    }

    private static IResult MapException(
        Exception exception)
    {
        return exception switch
        {
            CategorySlugAlreadyExistsException =>
                Results.Conflict(
                    new
                    {
                        code =
                            "category_slug_already_exists",

                        message =
                            exception.Message
                    }),

            CategoryParentNotFoundException =>
                Results.BadRequest(
                    new
                    {
                        code =
                            "category_parent_not_found",

                        message =
                            "The requested parent category was not found."
                    }),

            CategoryHierarchyCycleException =>
                Results.BadRequest(
                    new
                    {
                        code =
                            "category_hierarchy_cycle",

                        message =
                            exception.Message
                    }),

            CategoryNotFoundException =>
                Results.NotFound(
                    new
                    {
                        code =
                            "category_not_found",

                        message =
                            exception.Message
                    }),

            TenantScopeViolationException =>
                Results.Forbid(),

            ArgumentException =>
                Results.BadRequest(
                    new
                    {
                        code =
                            "validation_error",

                        message =
                            exception.Message
                    }),

            _ =>
                throw exception
        };
    }

    private static IResult InvalidCategoryId()
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
            result.CreatedAtUtc,
            result.ImageUrl);
    }
}
