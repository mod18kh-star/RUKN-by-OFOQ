namespace OFOQ.Market.Contracts.Commerce.Carts;

public sealed record CartItemResponse(
    Guid Id,
    Guid ProductId,
    Guid ProductVariantId,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal);