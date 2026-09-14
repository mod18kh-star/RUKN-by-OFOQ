using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.ProductImages;

public sealed class GetProductImagesHandler
{
    private readonly IProductRepository
        _productRepository;

    private readonly IProductImageRepository
        _imageRepository;

    public GetProductImagesHandler(
        IProductRepository productRepository,
        IProductImageRepository imageRepository)
    {
        _productRepository =
            productRepository;

        _imageRepository =
            imageRepository;
    }

    public async Task<ProductImagesResult?> HandleAsync(
        ProductId productId,
        CancellationToken cancellationToken = default)
    {
        var product =
            await _productRepository.GetByIdAsync(
                productId,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        var images =
            await _imageRepository.GetByProductIdAsync(
                productId,
                cancellationToken);

        return Map(
            productId,
            images);
    }

    internal static ProductImagesResult Map(
        ProductId productId,
        IReadOnlyCollection<ProductImage> images)
    {
        return new ProductImagesResult(
            productId.Value,
            images
                .OrderBy(
                    image =>
                        image.SortOrder)
                .Select(
                    image =>
                        new ProductImageResult(
                            image.Id.Value,
                            image.Url,
                            image.AltText,
                            image.SortOrder,
                            image.IsPrimary))
                .ToArray());
    }
}