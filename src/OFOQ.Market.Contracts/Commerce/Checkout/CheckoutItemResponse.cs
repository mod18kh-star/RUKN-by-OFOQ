namespace OFOQ.Market.Contracts.Commerce.Checkout;

public sealed record CheckoutItemResponse(
    Guid Id,
    Guid ProductId,
    Guid ProductVariantId,
    string ProductName,
    string VariantName,
    string Sku,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal);
