using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Domain.Commerce.Orders;

public sealed record OrderItemSnapshot(
    ProductId ProductId,
    ProductVariantId ProductVariantId,
    string ProductName,
    string VariantName,
    string Sku,
    Money UnitPrice,
    int Quantity);