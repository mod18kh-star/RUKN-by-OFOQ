namespace OFOQ.Market.Contracts.Catalog;

public sealed record ProductImageResponse(
    Guid ImageId,
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary);

public sealed record ProductImagesResponse(
    Guid ProductId,
    IReadOnlyList<ProductImageResponse> Images);

public sealed record SetProductImageRequest(
    string Url,
    string? AltText,
    bool IsPrimary);

public sealed record SetProductImagesRequest(
    IReadOnlyCollection<SetProductImageRequest> Images);