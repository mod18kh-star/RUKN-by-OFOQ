namespace OFOQ.Market.Contracts.Commerce.Carts;

public sealed record CartItemResponse(
    Guid Id,
    Guid ProductId,
    Guid ProductVariantId,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal,
    string? ProductName = null,
    string? ProductSlug = null,
    string? VariantName = null,
    string? PrimaryImageUrl = null,
    string? PrimaryImageAltText = null,
    decimal? CompareAtPrice = null);