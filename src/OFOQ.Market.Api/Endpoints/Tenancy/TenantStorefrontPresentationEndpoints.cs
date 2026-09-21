using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Application.Tenancy.StorefrontPresentation;
using OFOQ.Market.Contracts.Tenancy;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Endpoints.Tenancy;

public static class TenantStorefrontPresentationEndpoints
{
    public static IEndpointRouteBuilder MapTenantStorefrontPresentationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/backoffice/storefront-presentation")
                .WithTags(
                    "Tenant Storefront Presentation")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantBackOffice);

        group.MapGet(
            "",
            GetAsync);

        group.MapPut(
            "",
            UpdateAsync);

        group.MapPost(
            "/asset",
            UploadAssetAsync)
            .DisableAntiforgery();

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        GetStorefrontPresentationHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(
                Map(
                    await handler.HandleAsync(
                        cancellationToken)));
        }
        catch (TenantScopeViolationException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> UpdateAsync(
        UpdateStorefrontPresentationRequest request,
        UpdateStorefrontPresentationHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actorUserId =
            GetActorUserId(
                httpContext);

        if (!actorUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new UpdateStorefrontPresentationCommand(
                        request.LogoUrl,
                        request.CoverImageUrl,
                        request.Announcement,
                        request.PrimaryColor,
                        request.AccentColor,
                        request.ThemePresetCode,
                        request.FontCode,
                        request.ShowCategoriesOnHome,
                        request.ShowProductsOnHome,
                        request.CategorySectionTitle,
                        request.ProductSectionTitle,
                        actorUserId.Value,
                        request.VisualContentJson),
                    cancellationToken);

            return Results.Ok(
                Map(
                    result));
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
                        "storefront_presentation_invalid",

                    message =
                        exception.Message
                });
        }
    }

    private static async Task<IResult> UploadAssetAsync(
        Guid tenantId,
        HttpRequest request,
        ICurrentTenant currentTenant,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsAvailable ||
            currentTenant.TenantId?.Value != tenantId)
        {
            return Results.Forbid();
        }

        var form =
            await request.ReadFormAsync(
                cancellationToken);

        var slot =
            form["slot"]
                .ToString()
                .Trim()
                .ToLowerInvariant();

        if (slot is not ("logo" or "cover"))
        {
            return Results.BadRequest(
                new
                {
                    code = "storefront_asset_slot_invalid",
                    message = "نوع الصورة غير مدعوم."
                });
        }

        var file =
            form.Files.GetFile(
                "file");

        if (file is null ||
            file.Length <= 0)
        {
            return Results.BadRequest(
                new
                {
                    code = "storefront_asset_required",
                    message = "يرجى اختيار صورة صالحة."
                });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return Results.BadRequest(
                new
                {
                    code = "storefront_asset_too_large",
                    message = "حجم الصورة يجب أن يكون أقل من 5MB."
                });
        }

        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            !file.ContentType.StartsWith(
                "image/",
                StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(
                new
                {
                    code = "storefront_asset_invalid_type",
                    message = "نوع الملف يجب أن يكون صورة."
                });
        }

        var extension =
            Path.GetExtension(
                file.FileName);

        var allowedExtensions = new[]
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".webp",
            ".svg"
        };

        if (string.IsNullOrWhiteSpace(extension) ||
            !allowedExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest(
                new
                {
                    code = "storefront_asset_invalid_extension",
                    message = "الامتداد المدعوم هو PNG أو JPG أو WEBP أو SVG."
                });
        }

        var tenantPath =
            tenantId.ToString("N");

        var relativeDirectory =
            Path.Combine(
                "public-uploads",
                "storefront",
                tenantPath,
                slot);

        var physicalDirectory =
            Path.Combine(
                environment.ContentRootPath,
                "App_Data",
                relativeDirectory);

        Directory.CreateDirectory(
            physicalDirectory);

        var fileName =
            $"{slot}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        var physicalPath =
            Path.Combine(
                physicalDirectory,
                fileName);

        await using (var stream =
                     File.Create(
                         physicalPath))
        {
            await file.CopyToAsync(
                stream,
                cancellationToken);
        }

        var assetUrl =
            $"/public-uploads/storefront/{tenantPath}/{slot}/{fileName}";

        return Results.Ok(
            new
            {
                assetUrl,
                slot
            });
    }

    private static StorefrontPresentationResponse Map(
        StorefrontPresentationResult result)
    {
        return new StorefrontPresentationResponse(
            result.TenantId,
            result.LogoUrl,
            result.CoverImageUrl,
            result.Announcement,
            result.PrimaryColor,
            result.AccentColor,
            result.ThemePresetCode,
            result.FontCode,
            result.ShowCategoriesOnHome,
            result.ShowProductsOnHome,
            result.CategorySectionTitle,
            result.ProductSectionTitle,
            result.VisualContentJson);
    }

    private static Guid? GetActorUserId(
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
            userId ==
                Guid.Empty)
        {
            return null;
        }

        return userId;
    }
}
