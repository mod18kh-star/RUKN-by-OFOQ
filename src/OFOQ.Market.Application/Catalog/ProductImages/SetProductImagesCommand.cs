using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.ProductImages;

public sealed record ProductImageInput(
    string Url,
    string? AltText,
    bool IsPrimary);

public sealed record SetProductImagesCommand(
    ProductId ProductId,
    IReadOnlyCollection<ProductImageInput> Images,
    UserId ActorUserId);