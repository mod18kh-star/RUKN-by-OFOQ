using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http.Features;
using OFOQ.Market.Api.Security.Uploads;
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

        const long maxFileBytes =
            5L * 1024L * 1024L;

        if (!request.HasFormContentType ||
            request.ContentLength is >
                maxFileBytes + 65536)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "storefront_asset_invalid_request",
                    message =
                        "صيغة طلب رفع الصورة أو حجمه غير صالح."
                });
        }

        var bodySizeFeature =
            request.HttpContext.Features
                .Get<IHttpMaxRequestBodySizeFeature>();

        if (bodySizeFeature is
            { IsReadOnly: false })
        {
            bodySizeFeature.MaxRequestBodySize =
                maxFileBytes + 65536;
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

        if (form.Files.Count != 1 ||
            file is null ||
            file.Length <= 0)
        {
            return Results.BadRequest(
                new
                {
                    code = "storefront_asset_required",
                    message = "يرجى اختيار صورة صالحة."
                });
        }

        if (file.Length > maxFileBytes)
        {
            return Results.BadRequest(
                new
                {
                    code = "storefront_asset_too_large",
                    message = "حجم الصورة يجب أن يكون أقل من 5MB."
                });
        }

        var content =
            new byte[(int)file.Length];

        await using (var source =
                     file.OpenReadStream())
        {
            var position = 0;

            while (position < content.Length)
            {
                var read =
                    await source.ReadAsync(
                        content.AsMemory(
                            position),
                        cancellationToken);

                if (read == 0)
                {
                    return Results.BadRequest(
                        new
                        {
                            code =
                                "storefront_asset_read_failed",
                            message =
                                "تعذر قراءة ملف الصورة كاملًا."
                        });
                }

                position += read;
            }
        }

        if (!PublicImageUploadValidator.TryValidate(
                file.FileName,
                file.ContentType,
                content,
                out var extension,
                out _))
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "storefront_asset_invalid_file",
                    message =
                        "ملف الصورة غير صالح. استخدم PNG أو JPG أو WEBP حقيقيًا بأبعاد آمنة. SVG غير مسموح للملفات العامة."
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

        await File.WriteAllBytesAsync(
            physicalPath,
            content,
            cancellationToken);

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
