namespace OFOQ.Market.Contracts.Commerce.Carts;

public sealed record AddToCartRequest(
    Guid ProductId,
    Guid ProductVariantId,
    int Quantity);