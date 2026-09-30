using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http.Features;
using OFOQ.Market.Api.Security.Uploads;
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

        group.MapPost(
            "/assets",
            UploadAssetAsync)
            .DisableAntiforgery();

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
            8L * 1024L * 1024L;

        if (!request.HasFormContentType ||
            request.ContentLength is >
                maxFileBytes + 65536)
        {
            return ValidationError(
                "صيغة طلب رفع الصورة أو حجمه غير صالح.");
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

        var file =
            form.Files.GetFile(
                "file");

        if (form.Files.Count != 1 ||
            file is null ||
            file.Length <= 0)
        {
            return ValidationError(
                "يرجى اختيار صورة صالحة.");
        }

        if (file.Length > maxFileBytes)
        {
            return ValidationError(
                "حجم صورة المنتج يجب أن يكون أقل من 8MB.");
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
                    return ValidationError(
                        "تعذر قراءة ملف الصورة كاملًا.");
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
            return ValidationError(
                "ملف الصورة غير صالح. استخدم PNG أو JPG أو WEBP حقيقيًا بأبعاد آمنة.");
        }

        var tenantFolder = tenantId.ToString("N");
        var directory = Path.Combine(
            environment.ContentRootPath,
            "App_Data",
            "public-uploads",
            "products",
            tenantFolder);

        Directory.CreateDirectory(directory);

        var fileName = $"product-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(directory, fileName);

        await File.WriteAllBytesAsync(
            physicalPath,
            content,
            cancellationToken);

        return Results.Ok(new
        {
            assetUrl = $"/public-uploads/products/{tenantFolder}/{fileName}"
        });
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