namespace OFOQ.Market.Application.Catalog.ProductImages;

public sealed record ProductImageResult(
    Guid ImageId,
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary);

public sealed record ProductImagesResult(
    Guid ProductId,
    IReadOnlyList<ProductImageResult> Images);