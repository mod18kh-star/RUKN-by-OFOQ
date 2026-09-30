using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http.Features;
using OFOQ.Market.Api.Security.Uploads;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Application.Content;
using OFOQ.Market.Contracts.Content;
using OFOQ.Market.Domain.Content;

namespace OFOQ.Market.Api.Endpoints.Content;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var pages = endpoints
            .MapGroup("/api/tenants/{tenantId:guid}/backoffice/pages")
            .WithTags("Content Pages")
            .RequireAuthorization(AuthorizationPolicies.TenantBackOffice);

        pages.MapGet("", GetPagesAsync);
        pages.MapPost("", CreatePageAsync);
        pages.MapPut("/{id:guid}", UpdatePageAsync);
        pages.MapDelete("/{id:guid}", DeletePageAsync);
        pages.MapPost("/asset", UploadPageAssetAsync).DisableAntiforgery();

        var nav = endpoints
            .MapGroup("/api/tenants/{tenantId:guid}/backoffice/navigation")
            .WithTags("Navigation")
            .RequireAuthorization(AuthorizationPolicies.TenantBackOffice);

        nav.MapGet("", GetNavigationAsync);
        nav.MapPost("", CreateNavigationAsync);
        nav.MapPut("/{id:guid}", UpdateNavigationAsync);
        nav.MapDelete("/{id:guid}", DeleteNavigationAsync);

        var storefront = endpoints
            .MapGroup("/api/storefront/{storeSlug}")
            .WithTags("Storefront Content");

        storefront.MapGet("/pages/{pageSlug}", GetStorefrontPageAsync);
        storefront.MapGet("/pages/{pageSlug}/statistics", GetStorefrontPageStatisticsAsync);
        storefront.MapGet("/navigation", GetStorefrontNavigationAsync);

        return endpoints;
    }

    private static async Task<IResult> GetPagesAsync(
        ContentManagementService service,
        CancellationToken cancellationToken)
        => Results.Ok((await service.GetPagesAsync(cancellationToken)).Select(Map).ToArray());

    private static async Task<IResult> CreatePageAsync(
        UpsertContentPageRequest request,
        ContentManagementService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor = Actor(httpContext);
        if (!actor.HasValue) return Results.Unauthorized();

        if (!TryPageKind(request.PageKind, out var kind, out var error))
        {
            return error!;
        }

        try
        {
            var result = await service.CreatePageAsync(
                request.Title,
                request.Slug,
                request.Body,
                request.SeoTitle,
                request.SeoDescription,
                kind,
                request.HeroImageUrl,
                request.ShowCustomerCount,
                request.ShowCompletedOrderCount,
                request.ShowUnitsSold,
                request.ShowAverageRating,
                request.ShowReviewCount,
                request.ShowCountryCount,
                request.Publish,
                actor.Value,
                cancellationToken);

            return Results.Ok(Map(result));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Validation("content_page_invalid", exception.Message);
        }
    }

    private static async Task<IResult> UpdatePageAsync(
        Guid id,
        UpsertContentPageRequest request,
        ContentManagementService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor = Actor(httpContext);
        if (!actor.HasValue) return Results.Unauthorized();

        if (!TryPageKind(request.PageKind, out var kind, out var error))
        {
            return error!;
        }

        try
        {
            var result = await service.UpdatePageAsync(
                ContentPageId.From(id),
                request.Title,
                request.Slug,
                request.Body,
                request.SeoTitle,
                request.SeoDescription,
                kind,
                request.HeroImageUrl,
                request.ShowCustomerCount,
                request.ShowCompletedOrderCount,
                request.ShowUnitsSold,
                request.ShowAverageRating,
                request.ShowReviewCount,
                request.ShowCountryCount,
                request.Publish,
                actor.Value,
                cancellationToken);

            return result is null ? Results.NotFound() : Results.Ok(Map(result));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Validation("content_page_invalid", exception.Message);
        }
    }

    private static async Task<IResult> DeletePageAsync(
        Guid id,
        ContentManagementService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return await service.DeletePageAsync(ContentPageId.From(id), cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Validation("content_page_in_use", exception.Message);
        }
    }

    private static async Task<IResult> UploadPageAssetAsync(
        Guid tenantId,
        HttpRequest request,
        ICurrentTenant currentTenant,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsAvailable || currentTenant.TenantId?.Value != tenantId)
        {
            return Results.Forbid();
        }

        const long maxFileBytes =
            8L * 1024L * 1024L;

        if (!request.HasFormContentType ||
            request.ContentLength is >
                maxFileBytes + 65536)
        {
            return Validation(
                "content_page_asset_invalid_request",
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
            return Validation(
                "content_page_asset_required",
                "يرجى اختيار صورة صالحة.");
        }

        if (file.Length > maxFileBytes)
        {
            return Validation(
                "content_page_asset_too_large",
                "حجم الصورة يجب أن يكون أقل من 8MB.");
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
                    return Validation(
                        "content_page_asset_read_failed",
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
            return Validation(
                "content_page_asset_invalid_file",
                "ملف الصورة غير صالح. استخدم PNG أو JPG أو WEBP حقيقيًا بأبعاد آمنة.");
        }

        var tenantPath = tenantId.ToString("N");
        var relativeDirectory = Path.Combine("public-uploads", "content", tenantPath, "pages");
        var physicalDirectory = Path.Combine(environment.ContentRootPath, "App_Data", relativeDirectory);
        Directory.CreateDirectory(physicalDirectory);

        var fileName = $"page-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(physicalDirectory, fileName);

        await File.WriteAllBytesAsync(
            physicalPath,
            content,
            cancellationToken);

        return Results.Ok(new
        {
            assetUrl = $"/public-uploads/content/{tenantPath}/pages/{fileName}"
        });
    }

    private static async Task<IResult> GetNavigationAsync(
        ContentManagementService service,
        CancellationToken cancellationToken)
        => Results.Ok((await service.GetNavigationAsync(cancellationToken)).Select(Map).ToArray());

    private static async Task<IResult> CreateNavigationAsync(
        UpsertNavigationItemRequest request,
        ContentManagementService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor = Actor(httpContext);
        if (!actor.HasValue) return Results.Unauthorized();
        if (!TryEnums(request, out var location, out var type, out var error)) return error!;

        try
        {
            return Results.Ok(Map(await service.CreateNavigationAsync(
                location,
                type,
                request.Label,
                request.TargetId,
                request.ExternalUrl,
                request.ParentItemId,
                request.Position,
                request.IsVisible,
                actor.Value,
                cancellationToken)));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Validation("navigation_invalid", exception.Message);
        }
    }

    private static async Task<IResult> UpdateNavigationAsync(
        Guid id,
        UpsertNavigationItemRequest request,
        ContentManagementService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor = Actor(httpContext);
        if (!actor.HasValue) return Results.Unauthorized();
        if (!TryEnums(request, out var location, out var type, out var error)) return error!;

        try
        {
            var result = await service.UpdateNavigationAsync(
                NavigationItemId.From(id),
                location,
                type,
                request.Label,
                request.TargetId,
                request.ExternalUrl,
                request.ParentItemId,
                request.Position,
                request.IsVisible,
                actor.Value,
                cancellationToken);

            return result is null ? Results.NotFound() : Results.Ok(Map(result));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Validation("navigation_invalid", exception.Message);
        }
    }

    private static async Task<IResult> DeleteNavigationAsync(
        Guid id,
        ContentManagementService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return await service.DeleteNavigationAsync(NavigationItemId.From(id), cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Validation("navigation_in_use", exception.Message);
        }
    }

    private static async Task<IResult> GetStorefrontPageAsync(
        string storeSlug,
        string pageSlug,
        IStorefrontContentQueryRepository query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await query.GetPublishedPageAsync(storeSlug, pageSlug, cancellationToken);
            return result is null
                ? Results.NotFound()
                : Results.Ok(new StorefrontPageResponse(
                    result.Id,
                    result.Title,
                    result.Slug,
                    result.Body,
                    result.SeoTitle,
                    result.SeoDescription,
                    result.PublishedAtUtc,
                    result.PageKind,
                    result.HeroImageUrl));
        }
        catch (ArgumentException exception)
        {
            return Validation("storefront_content_invalid", exception.Message);
        }
    }

    private static async Task<IResult> GetStorefrontPageStatisticsAsync(
        string storeSlug,
        string pageSlug,
        IStorefrontContentQueryRepository query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await query.GetPublishedPageStatisticsAsync(storeSlug, pageSlug, cancellationToken);
            return result is null
                ? Results.NotFound()
                : Results.Ok(new StorefrontPageStatisticsResponse(
                    result.CustomerCount,
                    result.CompletedOrderCount,
                    result.UnitsSold,
                    result.AverageRating,
                    result.ReviewCount,
                    result.CountryCount));
        }
        catch (ArgumentException exception)
        {
            return Validation("storefront_statistics_invalid", exception.Message);
        }
    }

    private static async Task<IResult> GetStorefrontNavigationAsync(
        string storeSlug,
        string? location,
        IStorefrontContentQueryRepository query,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<NavigationLocation>(location ?? "Header", true, out var parsedLocation) ||
            !Enum.IsDefined(parsedLocation))
        {
            return Validation("navigation_location_invalid", "Unsupported navigation location.");
        }

        try
        {
            var result = await query.GetNavigationAsync(storeSlug, parsedLocation, cancellationToken);
            return result is null
                ? Results.NotFound()
                : Results.Ok(result.Select(item => new StorefrontNavigationResponse(
                    item.Id,
                    item.Location,
                    item.Type,
                    item.Label,
                    item.TargetId,
                    item.ExternalUrl,
                    item.ParentItemId,
                    item.SortOrder,
                    item.Href)).ToArray());
        }
        catch (ArgumentException exception)
        {
            return Validation("storefront_content_invalid", exception.Message);
        }
    }

    private static bool TryPageKind(
        string? value,
        out ContentPageKind kind,
        out IResult? error)
    {
        error = null;
        if (!Enum.TryParse(value ?? "Standard", true, out kind) || !Enum.IsDefined(kind))
        {
            error = Validation("content_page_kind_invalid", "نوع الصفحة غير مدعوم.");
            return false;
        }

        return true;
    }

    private static bool TryEnums(
        UpsertNavigationItemRequest request,
        out NavigationLocation location,
        out NavigationTargetType type,
        out IResult? error)
    {
        error = null;
        if (!Enum.TryParse(request.Location, true, out location) || !Enum.IsDefined(location))
        {
            type = default;
            error = Validation("navigation_location_invalid", "Unsupported navigation location.");
            return false;
        }

        if (!Enum.TryParse(request.Type, true, out type) || !Enum.IsDefined(type))
        {
            error = Validation("navigation_type_invalid", "Unsupported navigation target type.");
            return false;
        }

        return true;
    }

    private static Guid? Actor(HttpContext httpContext)
        => Guid.TryParse(httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) && id != Guid.Empty
            ? id
            : null;

    private static ContentPageResponse Map(ContentPageResult result)
        => new(
            result.Id,
            result.Title,
            result.Slug,
            result.Body,
            result.SeoTitle,
            result.SeoDescription,
            result.IsPublished,
            result.PublishedAtUtc,
            result.CreatedAtUtc,
            result.PageKind,
            result.HeroImageUrl,
            result.ShowCustomerCount,
            result.ShowCompletedOrderCount,
            result.ShowUnitsSold,
            result.ShowAverageRating,
            result.ShowReviewCount,
            result.ShowCountryCount);

    private static NavigationItemResponse Map(NavigationItemResult result)
        => new(
            result.Id,
            result.Location,
            result.Type,
            result.Label,
            result.TargetId,
            result.ExternalUrl,
            result.ParentItemId,
            result.SortOrder,
            result.IsVisible);

    private static IResult Validation(string code, string message)
        => Results.BadRequest(new { code, message });
}
