using OFOQ.Market.Application.Storefront;
using OFOQ.Market.Contracts.Storefront;

namespace OFOQ.Market.Api.Endpoints.Catalog;

public static class StorefrontEndpoints
{
    public static IEndpointRouteBuilder MapStorefrontEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/storefront/{storeSlug}")
                .WithTags(
                    "Storefront");

        group.MapGet(
            "",
            GetStoreAsync);

        group.MapGet(
            "/categories",
            GetCategoriesAsync);

        group.MapGet(
            "/products",
            GetProductsAsync);

        group.MapGet(
            "/products/{productSlug}",
            GetProductAsync);

        return endpoints;
    }

    private static async Task<IResult> GetStoreAsync(
        string storeSlug,
        StorefrontQueryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await service.GetStoreAsync(
                    storeSlug,
                    cancellationToken);

            return result is null
                ? StoreNotFound()
                : Results.Ok(
                    Map(
                        result));
        }
        catch (ArgumentException exception)
        {
            return InvalidQuery(
                exception.Message);
        }
    }

    private static async Task<IResult> GetCategoriesAsync(
        string storeSlug,
        StorefrontQueryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await service.GetCategoriesAsync(
                    storeSlug,
                    cancellationToken);

            if (result is null)
            {
                return StoreNotFound();
            }

            return Results.Ok(
                result
                    .Select(
                        item =>
                            new StorefrontCategoryResponse(
                                item.CategoryId,
                                item.Name,
                                item.Slug,
                                item.ParentCategoryId,
                                item.SortOrder))
                    .ToArray());
        }
        catch (ArgumentException exception)
        {
            return InvalidQuery(
                exception.Message);
        }
    }

    private static async Task<IResult> GetProductsAsync(
        string storeSlug,
        string? search,
        string? category,
        int? page,
        int? pageSize,
        StorefrontQueryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await service.GetProductsAsync(
                    storeSlug,
                    search,
                    category,
                    page ?? 1,
                    pageSize ?? 24,
                    cancellationToken);

            if (result is null)
            {
                return StoreNotFound();
            }

            return Results.Ok(
                new StorefrontProductPageResponse(
                    result.Page,
                    result.PageSize,
                    result.TotalCount,
                    result.TotalPages,
                    result.Items
                        .Select(
                            Map)
                        .ToArray()));
        }
        catch (ArgumentException exception)
        {
            return InvalidQuery(
                exception.Message);
        }
    }

    private static async Task<IResult> GetProductAsync(
        string storeSlug,
        string productSlug,
        StorefrontQueryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var lookup =
                await service.GetProductAsync(
                    storeSlug,
                    productSlug,
                    cancellationToken);

            if (!lookup.StoreExists)
            {
                return StoreNotFound();
            }

            if (lookup.Product is null)
            {
                return Results.NotFound(
                    new
                    {
                        code =
                            "storefront_product_not_found",

                        message =
                            "The requested product was not found."
                    });
            }

            var result =
                lookup.Product;

            return Results.Ok(
                new StorefrontProductDetailResponse(
                    result.ProductId,
                    result.Name,
                    result.Slug,
                    result.Description,
                    result.CategoryId,
                    result.CategoryName,
                    result.CategorySlug,
                    result.Price,
                    result.Currency,
                    result.CompareAtPrice,
                    result.AvailableForSale,
                    result.PrimaryImageUrl,
                    result.PrimaryImageAltText,
                    result.Images
                        .Select(
                            image =>
                                new StorefrontProductImageResponse(
                                    image.ImageId,
                                    image.Url,
                                    image.AltText,
                                    image.SortOrder,
                                    image.IsPrimary))
                        .ToArray(),
                    result.Variants
                        .Select(
                            variant =>
                                new StorefrontVariantResponse(
                                    variant.VariantId,
                                    variant.Name,
                                    variant.Sku,
                                    variant.IsDefault,
                                    variant.Price,
                                    variant.Currency,
                                    variant.TrackInventory,
                                    variant.Quantity,
                                    variant.ContinueSellingWhenOutOfStock,
                                    variant.AvailableForSale))
                        .ToArray(),
                    result.Attributes
                        .Select(
                            attribute =>
                                new StorefrontProductAttributeResponse(
                                    attribute.Key,
                                    attribute.Label,
                                    attribute.ValueType,
                                    attribute.Value))
                        .ToArray()));
        }
        catch (ArgumentException exception)
        {
            return InvalidQuery(
                exception.Message);
        }
    }

    private static StorefrontInfoResponse Map(
        StorefrontInfoResult result)
    {
        return new StorefrontInfoResponse(
            result.Name,
            result.Slug,
            result.Vertical,
            result.VerticalCode);
    }

    private static StorefrontProductSummaryResponse Map(
        StorefrontProductSummaryResult result)
    {
        return new StorefrontProductSummaryResponse(
            result.ProductId,
            result.Name,
            result.Slug,
            result.Description,
            result.CategoryId,
            result.Price,
            result.Currency,
            result.CompareAtPrice,
            result.AvailableForSale,
            result.PrimaryImageUrl,
            result.PrimaryImageAltText);
    }

    private static IResult StoreNotFound()
    {
        return Results.NotFound(
            new
            {
                code =
                    "storefront_not_found",

                message =
                    "The requested storefront was not found."
            });
    }

    private static IResult InvalidQuery(
        string message)
    {
        return Results.BadRequest(
            new
            {
                code =
                    "storefront_query_invalid",

                message
            });
    }
}