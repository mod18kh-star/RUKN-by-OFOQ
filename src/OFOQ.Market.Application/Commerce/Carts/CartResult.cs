using OFOQ.Market.Domain.Commerce.Carts;

namespace OFOQ.Market.Application.Commerce.Carts;

public sealed record CartResult(
    CartId Id,
    string? Currency,
    int TotalQuantity,
    decimal TotalAmount,
    IReadOnlyList<CartItemResult> Items);