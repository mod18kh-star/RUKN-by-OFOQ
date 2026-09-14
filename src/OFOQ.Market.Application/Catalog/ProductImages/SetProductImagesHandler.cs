using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.ProductImages;

public sealed class SetProductImagesHandler
{
    private const int MaximumImages =
        12;

    private readonly IProductRepository
        _productRepository;

    private readonly IProductImageRepository
        _imageRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public SetProductImagesHandler(
        IProductRepository productRepository,
        IProductImageRepository imageRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _productRepository =
            productRepository;

        _imageRepository =
            imageRepository;

        _currentTenant =
            currentTenant;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<ProductImagesResult?> HandleAsync(
        SetProductImagesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        EnsureTenant();

        ArgumentNullException.ThrowIfNull(
            command.Images);

        if (command.Images.Count >
            MaximumImages)
        {
            throw new ArgumentException(
                $"A product cannot have more than {MaximumImages} images.");
        }

        if (command.Images.Count == 1)
        {
            throw new ArgumentException(
                "A product image gallery must contain at least two images.");
        }

        if (command.Images.Count >
                0 &&
            command.Images.Count(
                image =>
                    image.IsPrimary) !=
                1)
        {
            throw new ArgumentException(
                "A product with images must have exactly one primary image.");
        }

        var duplicateUrl =
            command.Images
                .Where(
                    image =>
                        !string.IsNullOrWhiteSpace(
                            image.Url))
                .GroupBy(
                    image =>
                        image.Url.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(
                    group =>
                        group.Count() >
                        1);

        if (duplicateUrl is not null)
        {
            throw new ArgumentException(
                "A product cannot contain the same image URL more than once.");
        }

        var product =
            await _productRepository.GetByIdAsync(
                command.ProductId,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        var existing =
            await _imageRepository.GetByProductIdAsync(
                command.ProductId,
                cancellationToken);

        if (existing.Count >
            0)
        {
            _imageRepository.RemoveRange(
                existing);
        }

        var now =
            _timeProvider.GetUtcNow();

        var images =
            command.Images
                .Select(
                    (input, index) =>
                        ProductImage.Create(
                            _currentTenant.TenantId!.Value,
                            command.ProductId,
                            input.Url,
                            input.AltText,
                            index,
                            input.IsPrimary,
                            now,
                            command.ActorUserId.Value))
                .ToArray();

        if (images.Length >
            0)
        {
            await _imageRepository.AddRangeAsync(
                images,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return GetProductImagesHandler.Map(
            command.ProductId,
            images);
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to manage product images.");
        }
    }
}