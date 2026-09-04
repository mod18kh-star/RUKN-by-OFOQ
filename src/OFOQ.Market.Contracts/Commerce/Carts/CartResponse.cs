namespace OFOQ.Market.Contracts.Commerce.Carts;

public sealed record CartResponse(
    Guid Id,
    string? Currency,
    int TotalQuantity,
    decimal TotalAmount,
    IReadOnlyList<CartItemResponse> Items);